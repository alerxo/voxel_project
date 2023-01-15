using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;

public class World : MonoBehaviour
{
    //  Test values

    public bool initializeChunkActive;

    [Space]

    //  Terrain and climate

    [SerializeField] private int seed;
    public int randomSeed { get; private set; }

    public Climate[] climates;
    public int oceanAmountPerSide;

    [Space]

    public Biome[,] biomeMap = new Biome[VoxelData.worldSizeInChunks + 2, VoxelData.worldSizeInChunks + 2];

    public Biome oceanReference;

    //  Spawn

    [SerializeField] private GameObject spawn;
    public int spawnedChunksCount { get; private set; }

    //  Voxel related

    public Material solidMaterial;
    public Material transparentMaterial;
    public Material liquidMaterial;

    public Block[] blocks;

    public Chunk[,] chunksInWorld = new Chunk[VoxelData.worldSizeInChunks, VoxelData.worldSizeInChunks];

    public List<ChunkCoordinate> chunksToInitialize = new List<ChunkCoordinate>();

    //  Threading related

    public Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();
    public List<Chunk> chunksToUpdateLodOne = new List<Chunk>();
    public List<Chunk> chunksToForceUpdateLodTwo = new List<Chunk>();

    public List<Chunk> chunksToPopulate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    private bool applyingModifications = false;

    private Thread ThreadedUpdate;
    public object ThreadedUpdate_Lock = new object();

    private void Start()
    {
        //  Starts a new threads for chunk updating, renderdistance checks and voxelmap populating

        Random.InitState(seed);
        randomSeed = Random.Range(0, 1000000);

        biomeMap = BiomeMap.GetBiomeMap(this);

        GenerateWorld();
        GetSpawnPosition();

        ThreadedUpdate = new Thread(new ThreadStart(ThreadedUpdate_2));
        ThreadedUpdate.Start();
    }

    private void Update()
    {    
        if (chunksToDraw.Count > 0)
        {
            if (chunksToDraw.Peek().IsEditable || chunksToDraw.Peek().lodOneObject != null)
            {
                if (chunksToDraw.Peek().lodOneObject != null)
                {
                    spawnedChunksCount++;
                }

                chunksToDraw.Dequeue().CreateMesh();
            }
        }
    }

    private void GetSpawnPosition()
    {
        //  Middle of world is half of width on x,z
        //  Loops through every y block on spawn x,z looking for the grass block, (which is on terrainheight)
        //  Sets spawn at terrainheight

        spawn.transform.position = new Vector3((VoxelData.worldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight, (VoxelData.worldSizeInChunks * VoxelData.chunkWidth) / 2f);

        for (int i = VoxelData.chunkHeight; i < 0; i--)
        {
            if(WorldTerrain.GetVoxelBlockType(this, new Vector3(spawn.transform.position.x, i, spawn.transform.position.z), null, 0, true) != 0)
            {
                spawn.transform.position = new Vector3(spawn.transform.position.x, i + 2, spawn.transform.position.z);
                return;
            }
        }
    }

    private void GenerateWorld()
    {
        //  starting from middle of world, ie half of the width in x and z
        //  Remove render distance value form starting loop and add it to ending loop,
        //  so we get square with width 2 renderdistances, ie renderdistance ín all directions

        int wz = VoxelData.worldSizeInChunks / 2;
        int rd = VoxelData.RenderDistanceInChunks + VoxelData.RenderDistanceLodTwoInChunks;

        for (int x = wz - rd; x < wz + rd; x++)
        {
            for (int z = wz - rd; z < wz + rd; z++)
            {
                InitializeChunk(x, z);
            }
        }
    }

    public void NewChunkLodOne(int x, int z)
    {
        //  Adds new chunk to chunk array and then creates lod one of chunk

        if (chunksInWorld[x, z] != null)
        {
            lock (ThreadedUpdate_Lock)
            {
                chunksToPopulate.Add(chunksInWorld[x, z]);
            }
        }
    }

    public void InitializeChunk(int x, int z)
    {
        //  Initializes first chunk in list and removes,
        //  adds to spawned chunks so other scripts can caluclate when to do something

        if (chunksInWorld[x, z] == null)
        {
            ChunkCoordinate newChunk = new ChunkCoordinate(x, z);
            chunksInWorld[x, z] = new Chunk(newChunk, this);
            chunksToInitialize.Add(newChunk);

            ChunkCoordinate cc = chunksToInitialize[0];

            chunksToInitialize.RemoveAt(0);
            chunksInWorld[cc.x, cc.z].Initialize();
        }
    }

    private void ThreadedUpdate_2()
    {
        //  Infinte while loop on seperate thread, very scary :(

        while (true)
        {
            if (chunksToPopulate.Count > 0)
            {
                CreateLodOne();

                //  When we try to create lod one to many times we get laggy, so we sleep
                Thread.Sleep(100);
            }
                

            if (!applyingModifications)
            {
                ApplyModifications();
            }
             
            if (chunksToForceUpdateLodTwo.Count > 0)
            {
                CreateLodTwo();
            }  
        }
    }

    private void OnDisable()
    {
        //  Aborts infinite loop when we shut down program just in case

        ThreadedUpdate.Abort();
    }

    private void OnDestroy()
    {
        //  Aborts infinite loop when we shut down program just in case

        ThreadedUpdate.Abort();
    }

    private void CreateLodOne()
    {
        //  Starts a new round of chunk populating when it can
        //  Looks for the first chunk thats not populated, ie not editable

        bool updated = false;
        int index = 0;

        lock (ThreadedUpdate_Lock)
        {
            while (!updated && index < chunksToPopulate.Count - 1)
            {
                if (chunksToPopulate[index].IsEditable == false)
                {
                    chunksToPopulate[index].PopulateVoxelMap();
                    chunksToPopulate.RemoveAt(index);
                    updated = true;
                }
                else
                {
                    index++;
                }
                   
            }
        }
    }

    private void CreateLodTwo()
    {
        //  Starts a new round of chunk updating when it can

        bool updated = false;
        int index = 0;

        lock (ThreadedUpdate_Lock)
        {
            while (!updated && index < chunksToForceUpdateLodTwo.Count - 1)
            {
                chunksToForceUpdateLodTwo[index].UpdateChunk(false);
                chunksToForceUpdateLodTwo.RemoveAt(index);
                updated = true;
            }
        }
    }

    private void ApplyModifications()
    {
        //  modifications is a queue of queues, ie, queue of structure (has multiple voxels, hence a queue by itself)
        //  takes the first structure from modifications and loops through it updating all chunks it is in

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
                    if (chunksInWorld[cc.x, cc.z] == null)
                    {
                        Debug.Log("Error in World/ApplyModifications: Chunk doesn't exist");
                    }

                    chunksInWorld[cc.x, cc.z].modifications.Enqueue(vm);
                }
            }
        }

        applyingModifications=false;
    }

    public ChunkCoordinate GetChunkCoordinateFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return new ChunkCoordinate(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return chunksInWorld[x, z];
    }

    public bool CheckIfVoxelTransparent(Vector3 pos, int _tempTerrainHeight)
    {
        //  First looks if the voxel is in world, if not in world, return true,
        //  (face generation scrip doesnt recognize adjacent chunks as in world sometimes and we get into first check when we shoudlnt, so we draw face just in case)
        //  Then check chunk who is in world to check if chunk has been loaded, if it has, use istransparent
        //  If it hasnt been loaded, use get voxel

        ChunkCoordinate thisChunk = new ChunkCoordinate(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y >= VoxelData.chunkHeight)
        {
            return true;
        }

        if (chunksInWorld[thisChunk.x, thisChunk.z] != null && chunksInWorld[thisChunk.x, thisChunk.z].IsEditable)
        {
            return blocks[chunksInWorld[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;
        }

        return blocks[WorldTerrain.GetVoxelBlockType(this, pos, null, _tempTerrainHeight, true)].isTransparent;
    }

    public bool CheckIfVoxelLiquid(Vector3 pos, int _tempTerrainHeight)
    {
        //  First looks if the voxel is in world, if not in world, return true,
        //  (face generation scrip doesnt recognize adjacent chunks as in world sometimes and we get into first check when we shoudlnt, so we draw face just in case)
        //  Then check chunk who is in world to check if chunk has been loaded, if it has, use isLiquid
        //  If it hasnt been loaded, use get voxel

        ChunkCoordinate thisChunk = new ChunkCoordinate(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y >= VoxelData.chunkHeight)
        {
            return false;
        }

        if (chunksInWorld[thisChunk.x, thisChunk.z] != null && chunksInWorld[thisChunk.x, thisChunk.z].IsEditable)
        {
            return blocks[chunksInWorld[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isLiquid;
        }

        return blocks[WorldTerrain.GetVoxelBlockType(this, pos, null, _tempTerrainHeight, true)].isLiquid;
    }

    public bool IsChunkInWorld(ChunkCoordinate coord)
    {
        //  Checks if chunk is inside world

        if (coord.x >= 0 && coord.x < VoxelData.worldSizeInChunks && coord.z >= 0 && coord.z < VoxelData.worldSizeInChunks)
            return true;
        else
            return false;
    }
}