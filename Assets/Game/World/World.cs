using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Unity.Netcode;

public class World : MonoBehaviour
{
    //  Terrain and climate

    [SerializeField] private int seed;
    public int randomSeed { get; private set; }

    public Climate[] climates;
    public float[] thresholds;
    public float falloff;

    [Space]

    [Header("Noise values\nfreqency = x, less means smaller detail\namplitude = y, more means steeper curves")]

    [Space]

    [Tooltip("Distance between samplepoints, bigger lumps")]
    public float noiseScale;
    [Tooltip("Each octave adds another layer of noise")]
    public int octaves;
    [Tooltip("Controls decrease in amplitude of octaves, how much the small features made from lacunarity influence the curve")]
    [Range(0f, 1f)]
    public float persistence;
    [Tooltip("Controls increase in frequence of octaves, small features on the curve")]
    public float lacunarity;

    [Space]

    //  Spawn

    [SerializeField] private GameObject spawn;
    public int spawnedChunksCount { get; private set; }

    //  Voxel related

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate>();

    public Material material;
    public Material transparentMaterial;
    public Block[] blocks;

    public Chunk[,] chunksInWorld = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    public List<ChunkCoordinate> chunksToInitialize = new List<ChunkCoordinate>();

    //  Threading related

    public Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
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

        ThreadedUpdate = new Thread(new ThreadStart(ThreadedUpdate_2));
        ThreadedUpdate.Start();

        GenerateWorld();
        GetSpawnPosition();
    }

    private void Update()
    {    
        if(chunksToInitialize.Count > 0)
            InitializeChunk();

        if(chunksToDraw.Count > 0)
        {
            if (chunksToDraw.Peek().IsEditable)
            {
                chunksToDraw.Dequeue().CreateMesh();
            }
        }
    }

    private void GetSpawnPosition()
    {
        //  Middle of world is half of width on x,z
        //  Loops through every y block on spawn x,z looking for the grass block, (which is on terrainheight)
        //  Sets spawn at terrainheight

        spawn.transform.position = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight, (VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f);

        for (int i = 0; i < VoxelData.chunkHeight; i++)
        {
            if(WorldTerrain.GetVoxelBlockType(this, new Vector3(spawn.transform.position.x, i, spawn.transform.position.z)) == 1)
            {
                spawn.transform.position = new Vector3(spawn.transform.position.x, i + 2, spawn.transform.position.z);
            }
        }
    }

    private void GenerateWorld()
    {
        //  starting from middle of world, ie half of the width in x and z
        //  Remove render distance value form starting loop and add it to ending loop,
        //  so we get square with width 2 renderdistances, ie renderdistance ín all directions

        for (int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.RenderDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.RenderDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.RenderDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.RenderDistanceInChunks; z++)
            {
                NewChunk(x, z);
            }
        }
    }   

    public void NewChunk(int x, int z)
    {
        //  Adds new chunk to chunk array and then initializes it

        if (chunksInWorld[x, z] == null)
        {
            ChunkCoordinate newChunk = new ChunkCoordinate(x, z);
            chunksInWorld[x, z] = new Chunk(newChunk, this);
            chunksToInitialize.Add(newChunk);
        }
    }

    private void InitializeChunk()
    {
        //  Initializes first chunk in list and removes,
        //  adds to spawned chunks so other scripts can caluclate when to do something

        ChunkCoordinate cc = chunksToInitialize[0];
        chunksToInitialize.RemoveAt(0);
        chunksInWorld[cc.x, cc.z].Initialize();

        spawnedChunksCount++;
    }

    private void ThreadedUpdate_2()
    {
        //  Infinte while loop on seperate thread, very scary :(
        //  Sleeps 10 ms after every loop cus we dont need to kill cpu

        while (true)
        {
            if (chunksToPopulate.Count > 0)
                PopulateChunks();

            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
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

    private void PopulateChunks()
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
                    index++;
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
                        chunksInWorld[cc.x, cc.z] = new Chunk(cc, this);
                        chunksToInitialize.Add(cc);
                    }

                    chunksInWorld[cc.x, cc.z].modifications.Enqueue(vm);
                }
            }
        }

        applyingModifications=false;
    }

    private void UpdateChunks()
    {
        //  Starts a new round of chunk updating when it can
        //  Looks for the first chunk thats populated, ie editable

        bool updated = false;
        int index = 0;

        lock (ThreadedUpdate_Lock)
        {
            while (!updated && index < chunksToUpdate.Count - 1)
            {
                if (chunksToUpdate[index].IsEditable)
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

    public bool CheckIfVoxelTransparent(Vector3 pos)
    {
        //  First looks if the voxel is in world, if not in world and an air voxel, return true,
        //  (face generation scrip doesnt recognize adjacent chunks as in world sometimes and we get into first check when we shoudlnt, so we draw face just in case)
        //  Then check chunk who is in world to check if chunk has been loaded, if it has, use istransparent
        //  If it hasnt been loaded, use get voxel

        ChunkCoordinate thisChunk = new ChunkCoordinate(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y >= VoxelData.chunkHeight)
        {
            //  If voxel is air, return true
            if (WorldTerrain.GetVoxelBlockType(this, pos) == 0)
                return true;
            else
                return false;
        }

        if (chunksInWorld[thisChunk.x, thisChunk.z] != null && chunksInWorld[thisChunk.x, thisChunk.z].IsEditable)
        {
            return blocks[chunksInWorld[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;
        }

        return blocks[WorldTerrain.GetVoxelBlockType(this, pos)].isTransparent;
    }

    public bool IsChunkInWorld(ChunkCoordinate coord)
    {
        //  Checks if chunk is inside world

        if (coord.x >= 0 && coord.x < VoxelData.WorldSizeInChunks && coord.z >= 0 && coord.z < VoxelData.WorldSizeInChunks)
            return true;
        else
            return false;
    }
}