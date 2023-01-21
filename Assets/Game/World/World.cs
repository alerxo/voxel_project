using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class World : MonoBehaviour
{
    #region oof

    //  Test values

    public bool initializeChunkActive;

    [Space]

    //  Terrain and climate

    [SerializeField] private int seed;
    public int randomSeed { get; private set; }

    public Biome[] biomes;

    public SubBiome[,] subBiomMap = new SubBiome[VoxelData.worldSizeInChunks + 2, VoxelData.worldSizeInChunks + 2];

    [Space]

    [Tooltip("How much detail between sample points")]
    public int resolution;
    [Tooltip("Distance between samplepoints, bigger lumps")]
    public float noiseScale;
    [Tooltip("Each octave adds another layer of noise")]
    public int octaves;
    [Tooltip("Controls decrease in amplitude of octaves, how much the small features made from lacunarity influence the curve")]
    [Range(0f, 1f)]
    public float persistence;
    [Tooltip("Controls increase in frequence of octaves, small features on the curve")]
    public float lacunarity;
    [Tooltip("Noise can not get higher than this")]
    public float noiseCutoff;

    [Space]

    //  Spawn

    [SerializeField] private GameObject spawn;

    //  Voxel related

    [Space]

    public Material solidMaterial;
    public Material transparentMaterial;
    public Material liquidMaterial;

    [Space]

    public Block[] blocks;

    public Chunk[,] chunksInWorld = new Chunk[VoxelData.worldSizeInChunks, VoxelData.worldSizeInChunks];

    public List<ChunkCoordinate> chunksToInitialize = new List<ChunkCoordinate>();

    //  Threading related

    public Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    public List<Chunk> chunksToCreateChunkData = new List<Chunk>();
    public List<Chunk> chunksToUpdateLodTwo = new List<Chunk>();

    public Queue<Chunk> chunksToDrawLodOne = new Queue<Chunk>();
    public Queue<Chunk> chunksToDrawWater = new Queue<Chunk>();
    public Queue<Chunk> chunksToDrawLodTwo = new Queue<Chunk>();

    private bool applyingModifications = false;

    private Thread UpdateChunk_Thread;
    public object ThreadedUpdate_Lock = new object();

    #endregion

    private void Start()
    {
        //  Starts a new threads for chunk updating, renderdistance checks and voxelmap populating

        RenderSettings.fogStartDistance = (VoxelData.RenderDistanceInChunks * VoxelData.chunkWidth) - VoxelData.chunkWidth * 6;
        RenderSettings.fogEndDistance = (VoxelData.RenderDistanceInChunks * VoxelData.chunkWidth) - VoxelData.chunkWidth * 2;

        Random.InitState(seed);
        randomSeed = Random.Range(0, 1000000);

        subBiomMap = BiomeMap.GetBiomeMap(this);

        GenerateWorld();
        GetSpawnPosition();

        UpdateChunk_Thread = new Thread(new ThreadStart(ThreadedUpdate));
        UpdateChunk_Thread.Start();
    }

    private void Update()
    {
        if (chunksToDrawLodOne.Count > 0)
        {
            if (chunksToDrawLodOne.Peek().IsEditable)
            {
                chunksToDrawLodOne.Dequeue().CreateMeshLodOne();
            }
        }

        else if (chunksToDrawWater.Count > 0)
        {
            if (chunksToDrawWater.Peek().IsEditable)
            {
                chunksToDrawWater.Dequeue().CreateMeshWater();
            }
        }

        else if (chunksToDrawLodTwo.Count > 0)
        {
            if (chunksToDrawLodTwo.Peek().IsEditable)
            {
                chunksToDrawLodTwo.Dequeue().CreateMeshLodTwo();
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
            if (WorldTerrain.GetVoxelBlockType(this, new Vector3(spawn.transform.position.x, i, spawn.transform.position.z), true, 0) != 0)
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

        int worldSize = VoxelData.worldSizeInChunks;

        for (int x = 0; x < worldSize; x++)
        {
            for (int z = 0; z < worldSize; z++)
            {
                InitializeChunk(x, z);
            }
        }
    }

    public void EnqueueNewChunk(int x, int z)
    {
        //  Adds new chunk to chunk array and then creates lod one of chunk

        if (chunksInWorld[x, z] != null)
        {
            lock (ThreadedUpdate_Lock)
            {
                chunksToCreateChunkData.Add(chunksInWorld[x, z]);
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

    private void ThreadedUpdate()
    {
        //  Infinte while loop on seperate thread, very scary :(

        while (true)
        {
            if (!applyingModifications)
            {
                ApplyModifications();
            }

            if (chunksToCreateChunkData.Count > 0)
            {
                CreateChunkData();
            }
        }
    }

    private void CreateChunkData()
    {
        //  Starts a new round of chunk populating when it can
        //  Looks for the first chunk thats not populated, ie not editable

        bool updated = false;
        int index = 0;

        lock (ThreadedUpdate_Lock)
        {
            while (!updated && index < chunksToCreateChunkData.Count - 1)
            {
                if (chunksToCreateChunkData[index].IsEditable == false)
                {
                    chunksToCreateChunkData[index].PopulateVoxelMap();
                    chunksToCreateChunkData.RemoveAt(index);
                    updated = true;
                }
                else
                {
                    index++;
                }
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

            while (modQueue.Count > 0)
            {
                VoxelMod vm = modQueue.Dequeue();

                ChunkCoordinate cc = Voxel_Util.GetChunkCoordinateFromVector3(vm.position);

                if (Voxel_Util.IsChunkInWorld(cc))
                {
                    if (chunksInWorld[cc.x, cc.z] == null)
                    {
                        Debug.Log("Error in World/ApplyModifications: Chunk doesn't exist");
                    }

                    chunksInWorld[cc.x, cc.z].modifications.Enqueue(vm);
                }
            }
        }

        applyingModifications = false;
    }

    private void OnDisable()
    {
        //  Aborts infinite loop when we shut down program just in case

        UpdateChunk_Thread.Abort();
    }

    private void OnDestroy()
    {
        //  Aborts infinite loop when we shut down program just in case

        UpdateChunk_Thread.Abort();
    }
}