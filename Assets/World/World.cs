using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class World : MonoBehaviour
{
    public int seed;
    public Biome biome;

    public Transform player;
    public Vector3 spawnPosition;

    public GameObject debugScreen;

    public Material material;
    public Material transparentMaterial;

    public Block[] blocks;

    private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate>();

    public ChunkCoordinate playerChunk;
    private ChunkCoordinate playerLastChunk;

    private List<ChunkCoordinate> chunksToInitialize = new List<ChunkCoordinate>();

    //  Threading related

    private Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public List<Chunk> chunksToPopulate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    private bool applyingModifications = false;

    private Thread Thread_2;
    public object Thread_2Lock = new object();

    private Thread Thread_3;
    public object Thrad_3Lock = new object();

    private bool hasCheckedRenderDistance = true;

    private Vector3 playerPosition;

    private Queue<Chunk> chunksToActivate = new Queue<Chunk>();
    private Queue<Chunk> chunksToDeactivate = new Queue<Chunk>();

    //

    private void Start()
    {
        //  Starts a new threads for chunk updating, renderdistance checks and voxelmap populating

        Random.InitState(seed);

        Thread_2 = new Thread(new ThreadStart(ThreadedUpdate_2));
        Thread_2.Start();

        Thread_3 = new Thread(new ThreadStart(ThreadedUpdate_3));
        Thread_3.Start();

        SpawnPlayer();
        GenerateWorld();
    }

    private void Update()
    {
        playerChunk = GetChunkCoordinateFromVector3 (player.position);

        if (!playerChunk.Equals(playerLastChunk))
        {
            playerPosition = player.position;
            hasCheckedRenderDistance = false;
        }

        if (chunksToActivate.Count > 0)
            ActivateChunks();

        if (chunksToDeactivate.Count > 0)
            DeactivateChunks();
            
        if(chunksToInitialize.Count > 0)
            InitializeChunk();

        if(chunksToDraw.Count > 0)
        {
            if (chunksToDraw.Peek().isEditable)
            {
                chunksToDraw.Dequeue().CreateMesh();
            }
        }

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);
    }

    private void SpawnPlayer()
    {
        //  Middle of world is half of width on x,z
        //  Sets playerlastchunk as spawnchunk
        //  Loops through every y block on spawn x,z looking for the grass block, (which is on terrainheight)
        //  When found spawn player

        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight, (VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f);
        playerLastChunk = GetChunkCoordinateFromVector3(spawnPosition);

        for (int i = 0; i < VoxelData.chunkHeight; i++)
        {
            if(GetVoxelBlockType(new Vector3(spawnPosition.x, i, spawnPosition.z)) == 1)
                player.position = new Vector3(spawnPosition.x, i + 2, spawnPosition.z);
        }
    }

    private void GenerateWorld()
    {
        //  Generates the world according to world size in chunks

        for (int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.RenderDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.RenderDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.RenderDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.RenderDistanceInChunks; z++)
            {
                ChunkCoordinate newChunk = new ChunkCoordinate(x, z);

                chunks[x, z] = new Chunk(newChunk, this);
                chunksToInitialize.Add(newChunk);
            }
        }
    }

    private void InitializeChunk()
    {
        //  Initializes first chunk in list and removes

        ChunkCoordinate cc = chunksToInitialize[0];
        chunksToInitialize.RemoveAt(0);
        chunks[cc.x, cc.z].Init();
    }

    private void ThreadedUpdate_2()
    {
        //  Infinte while loop on seperate thread, very scary :(

        while (true)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }
    }

    private void ThreadedUpdate_3()
    {
        //  Infinte while loop on seperate thread, very scary :(

        while (true)
        {
            if (!hasCheckedRenderDistance)
                CheckRenderDistance();

            if (chunksToPopulate.Count > 0)
                PopulateChunks();
        }
    }

    private void OnDisable()
    {
        //  Aborts infinite loop when we shut down program just in case

        Thread_2.Abort();
        Thread_3.Abort();
    }

    private void CheckRenderDistance()
    {
        //  Checks if there are any chunks in render distance that should exist but dont
        //  If so, generate
        //  Else if it exists but not active, tell main thread to activate
        //  Then check if any old chunks are outside of render distance
        //  If so, tell main thread to deactivate

        ChunkCoordinate coord = GetChunkCoordinateFromVector3(playerPosition);
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
                        chunks[x, z] = new Chunk(new ChunkCoordinate(x, z), this);
                        chunksToInitialize.Add(new ChunkCoordinate(x, z));
                    }

                    else if (!chunks[x, z].isActive)
                    {
                        chunksToActivate.Enqueue(chunks[x, z]);
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


            chunksToDeactivate.Enqueue(chunks[c.x, c.z]);
        }

        hasCheckedRenderDistance = true;
    }

    private void PopulateChunks()
    {
        //  Starts a new round of chunk populating when it can

        bool updated = false;
        int index = 0;

        lock (Thread_2Lock)
        {
            while (!updated && index < chunksToPopulate.Count - 1)
            {
                if (chunksToPopulate[index].isEditable == false)
                {
                    chunksToPopulate[index].PopulateVoxelMap();
                    chunksToPopulate.RemoveAt(index);
                    updated = true;
                }
                else
                    index++;
            }
        }
    }

    private void ApplyModifications()
    {
        //  Taking a modification from queue and doing it

        applyingModifications = true;

        while (modifications.Count > 0)
        {
            Queue<VoxelMod> modQueue = modifications.Dequeue();

            while(modQueue.Count > 0)
            {
                VoxelMod vm = modQueue.Dequeue();

                ChunkCoordinate cc = GetChunkCoordinateFromVector3(vm.position);

                if (IsChunkInWorld(cc))
                {
                    if (chunks[cc.x, cc.z] == null)
                    {
                        chunks[cc.x, cc.z] = new Chunk(cc, this);
                        chunksToInitialize.Add(cc);
                    }

                    chunks[cc.x, cc.z].modifications.Enqueue(vm);
                }
            }
        }

        applyingModifications=false;
    }

    private void UpdateChunks()
    {
        //  Starts a new round of chunk updating when it can

        bool updated = false;
        int index = 0;

        lock (Thread_2Lock)
        {
            while (!updated && index < chunksToUpdate.Count - 1)
            {
                if (chunksToUpdate[index].isEditable)
                {
                    chunksToUpdate[index].UpdateChunk();
                    activeChunks.Add(chunksToUpdate[index].coord);
                    chunksToUpdate.RemoveAt(index);
                    updated = true;
                }
                else
                    index++;
            }
        }
    }

    private void ActivateChunks()
    {
        while (chunksToActivate.Count > 0)
            chunksToActivate.Dequeue().isActive = true;
    }

    private void DeactivateChunks()
    {
        while (chunksToDeactivate.Count > 0)
            chunksToDeactivate.Dequeue().isActive = false;
    }

    private ChunkCoordinate GetChunkCoordinateFromVector3(Vector3 pos)
    {
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
            if (GetVoxelBlockType(pos) == 0)
                return true;


            else
                return false;
        }

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
        {
            return blocks[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;
        }

        return blocks[GetVoxelBlockType(pos)].isTransparent;
    }

    public byte GetVoxelBlockType(Vector3 pos)
    {
        //  World generation basically
        //  Has few parameters per block of code, uses noise to see if it passes parameters, and assigns block thereafter

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
                    modifications.Enqueue(Structure.MakeTree(new Vector3(pos.x, pos.y, pos.z), biome.minTreeY, biome.maxTreeY));
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