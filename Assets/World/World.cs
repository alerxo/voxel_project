using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class World : MonoBehaviour
{
    public int seed;
    public Biome biome;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Block[] blocks;

    private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate> ();

    public ChunkCoordinate playerChunk;
    private ChunkCoordinate playerLastChunk;

    private List<ChunkCoordinate> chunkQueue = new List<ChunkCoordinate>();

    private bool isCreatingChunks;

    public GameObject debugScreen;

    private void Start()
    {
        Random.InitState(seed);

        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight + 2f, (VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f);

        GenerateWorld();

        playerLastChunk = GetChunkCoordinateFromVector3 (player.position);
    }

    private void Update()
    {
        //  Checks for player movement between chunks
        //  Starts the chunk creation if needed

        playerChunk = GetChunkCoordinateFromVector3 (player.position);

        if (!playerChunk.Equals(playerLastChunk))
            CheckRenderDistance();

        if(chunkQueue.Count > 0 && !isCreatingChunks)
            StartCoroutine("CreateChunks");

        if (Input.GetKeyDown(KeyCode.F3))
        {
            debugScreen.SetActive(!debugScreen.activeSelf);
        }
    }

    private void GenerateWorld()
    {
        //  Generates the world according to world size in chunks
        //  Moves player to spawn

        for(int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.RenderDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.RenderDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.RenderDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.RenderDistanceInChunks; z++)
            {
                chunks[x, z] = new Chunk(new ChunkCoordinate(x, z), this, true);
                activeChunks.Add(new ChunkCoordinate(x, z));
            }
        }

        player.position = spawnPosition;
    }

    private IEnumerator CreateChunks()
    {
        //  Starts initialzing the first chunk in chunk queue
        //  yields to rest of code after one chunk is created
        //  does another chunk next frame
        //  loooooop

        isCreatingChunks = true;

        while(chunkQueue.Count > 0)
        {
            chunks[chunkQueue[0].x, chunkQueue[0].z].Init();
            chunkQueue.RemoveAt(0);

            yield return null;
        }

        isCreatingChunks = false;
    }

    private ChunkCoordinate GetChunkCoordinateFromVector3(Vector3 pos)
    {
        //  Gets the chunk coordinate from a position

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return new ChunkCoordinate(x, z);
    }

    private void CheckRenderDistance()
    {
        //  Checks if there are any chunks in render distance that should exist but dont
        //  If so, generate
        //  Else if it exists but not active, activate
        //  Then check if any old chunks are outside of render distance
        //  If so, deactivate

        ChunkCoordinate coord = GetChunkCoordinateFromVector3(player.position);
        playerLastChunk = playerChunk;

        List<ChunkCoordinate> previoslyActiveChunks = new List<ChunkCoordinate>(activeChunks);

        for (int x = coord.x - VoxelData.RenderDistanceInChunks; x < coord.x + VoxelData.RenderDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.RenderDistanceInChunks; z < coord.z + VoxelData.RenderDistanceInChunks; z++)
            {
                if (IsChunkInWorld(new ChunkCoordinate(x, z)))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(new ChunkCoordinate(x, z), this, false);
                        chunkQueue.Add(new ChunkCoordinate(x, z));
                    }

                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                        
                    }

                    activeChunks.Add(new ChunkCoordinate(x, z));
                }

                for (int i = 0; i < previoslyActiveChunks.Count; i++)
                {
                    if (previoslyActiveChunks[i].Equals(new ChunkCoordinate(x, z)))
                        previoslyActiveChunks.RemoveAt(i);
                    
                }
            }
        }

        foreach (ChunkCoordinate c in previoslyActiveChunks)
        {
            activeChunks.Remove(new ChunkCoordinate(c.x, c.z));
            chunks[c.x, c.z].isActive = false;
        }
            
    }

    public bool CheckForVoxel(Vector3 pos)
    {
        ChunkCoordinate thisChunk = new ChunkCoordinate(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.chunkHeight)
        {
            return false;
        }

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
        {
            return blocks[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        }

        return blocks[GetVoxel(pos)].isSolid;
    }

    public byte GetVoxel(Vector3 pos)
    {
        int yPos = Mathf.FloorToInt(pos.y);

        //  Constant

        if (!IsVoxelInWorld(pos))
        {
            //  If outside world, return air
            return 0;
        }

        //  Terrain

        int terrainheight = Mathf.FloorToInt((biome.maxGroundY - biome.minGroundY) * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.minGroundY;

        byte voxelValue = 0;


        if (yPos == terrainheight)
            voxelValue = 1;
        else if (yPos < terrainheight && yPos > terrainheight - 4)
            voxelValue = 3;
        else if (yPos < terrainheight)
            voxelValue = 2;
        else
            voxelValue = 0;

        //  Second

        if (voxelValue == 0)
        {
            foreach(Lode lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
            }
        }

        return voxelValue;
    }   



    private bool IsChunkInWorld(ChunkCoordinate coord)
    {
        //  Checks if chunk is inside world

        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks)
            return true;
        else
            return false;
    }

    private bool IsVoxelInWorld(Vector3 pos)
    {
        //  Checks if voxel is inside world

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxel && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxel)
            return true;
        else
            return false;
    }
}