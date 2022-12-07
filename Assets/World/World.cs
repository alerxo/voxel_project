using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class World : MonoBehaviour
{
    public int seed;
    public Biome biome;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;

    public Block[] blocks;

    private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate>();

    public ChunkCoordinate playerChunk;
    private ChunkCoordinate playerLastChunk;

    private List<ChunkCoordinate> chunkQueue = new List<ChunkCoordinate>();

    public GameObject debugScreen;

    private Queue<VoxelMod> modifications = new Queue<VoxelMod>();
    private List<Chunk> chunksToModify = new List<Chunk>();

    private bool applyingModifications = false;

    private void Start()
    {
        Random.InitState(seed);

        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight + 2f, (VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f);

        GenerateWorld();

        playerLastChunk = GetChunkCoordinateFromVector3 (player.position);
    }

    private void Update()
    {
        playerChunk = GetChunkCoordinateFromVector3 (player.position);

        if (!playerChunk.Equals(playerLastChunk))
            CheckRenderDistance();

        if (modifications.Count > 0 && !applyingModifications)
            StartCoroutine(ApplyModifications());

        if(chunkQueue.Count > 0)
            CreateChunk();

        if (chunksToModify.Count > 0)
            UpdateChunk();

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);
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

        while(modifications.Count > 0)
        {
            //  Good practic to lock in future

            VoxelMod vm = modifications.Dequeue();

            ChunkCoordinate cc = GetChunkCoordinateFromVector3(vm.position);

            if (chunks[cc.x, cc.z] == null)
            {
                chunks[cc.x, cc.z] = new Chunk(cc, this, true);
                activeChunks.Add(cc);
            }

            chunks[cc.x, cc.z].modifications.Enqueue(vm);

            if (!chunksToModify.Contains(chunks[cc.x, cc.z]))
                chunksToModify.Add(chunks[cc.x, cc.z]);

            for(int i = 0; i < chunksToModify.Count; i++)
            {
                chunksToModify[0].UpdateChunk();
                chunksToModify.RemoveAt(0);
            }
        }

        player.position = spawnPosition;
    }

    private void CreateChunk()
    {
        //  Initializes chunk

        ChunkCoordinate cc = chunkQueue[0];
        chunkQueue.RemoveAt(0);
        activeChunks.Add(cc);
        chunks[cc.x, cc.z].Init();
    }

    private void UpdateChunk()
    {
        bool updated = false;
        int index = 0;

        while (!updated && index < chunksToModify.Count - 1)
        {
            if (chunksToModify[index].isVoxelMapPopulated)
            {
                chunksToModify[index].UpdateChunk();
                chunksToModify.RemoveAt(index);
                updated = true;
            }
            else
                index++;
        }
    }

    private IEnumerator ApplyModifications()
    {
        //  Taking a modification from queue and doing it
        //  Yields after x amount as to not bog down system

        applyingModifications = true;
        int count = 0;

        while (modifications.Count > 0)
        {
            VoxelMod vm = modifications.Dequeue();

            ChunkCoordinate cc = GetChunkCoordinateFromVector3(vm.position);

            if (IsChunkInWorld(cc))
            {
                if (chunks[cc.x, cc.z] == null)
                {
                    chunks[cc.x, cc.z] = new Chunk(cc, this, true);
                    activeChunks.Add(cc);
                }

                chunks[cc.x, cc.z].modifications.Enqueue(vm);

                if (!chunksToModify.Contains(chunks[cc.x, cc.z]))
                    chunksToModify.Add(chunks[cc.x, cc.z]);

                count++;
                if (count > 200)
                {
                    count = 0;
                    yield return null;

                }
            }
        }

        applyingModifications=false;
    }

    private ChunkCoordinate GetChunkCoordinateFromVector3(Vector3 pos)
    {
        //  Gets the chunk coordinate from a position

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return new ChunkCoordinate(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return chunks[x, z];
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

        activeChunks.Clear();

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

    public bool CheckIfVoxelTransparent(Vector3 pos)
    {
        //  First looks if the voxel is in world, if not in world and an air voxel, return true,
        //  (face generation scrip doesnt recognize adjacent chunks as in world sometimes and we get into first check when we shoudlnt, so we draw face just in case)
        //  Then check chunk who is in world to check if chunk has been loaded, if it has, use istransparent
        //  If it hasnt been loaded, use get voxel

        ChunkCoordinate thisChunk = new ChunkCoordinate(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.chunkHeight)
        {
            //  If voxel is air, return true
            if (GetVoxel(pos) == 0)
                return true;


            else
                return false;
        }

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
        {
            return blocks[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;
        }

        return blocks[GetVoxel(pos)].isTransparent;
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
            voxelValue = 2;
        else if (yPos < terrainheight)
            voxelValue = 3;
        else
            voxelValue = 0;

        //  Lode

        if (voxelValue == 0)
        {
            foreach(Lode lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
            }
        }

        //  Tree

        if(yPos == terrainheight + 1)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 10000, biome.treeZoneScale) > biome.treeZoneThreshHold)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 20000, biome.treePlacementScale) > biome.treePlacementThreshold)
                {
                    voxelValue = 5;
                    Structure.MakeTree(new Vector3(pos.x, pos.y, pos.z), modifications, biome.minTreeY, biome.maxTreeY);
                }
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