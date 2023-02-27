using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;

public class World : MonoBehaviour
{
    //  Terrain and climate

    public Biome[] biomes;
    public Town[] towns;
    public Dungeon[] dungeons;
    public SubBiome[,] subBiomeMap = new SubBiome[World_Settings.worldSizeInChunks + 2, World_Settings.worldSizeInChunks + 2];

    [Space]

    public SubBiome riverReference;
   
    //  Spawn

    [SerializeField] private GameObject spawn;

    //  Voxel related

    [Space]

    public Material solidMaterial;
    public Material transparentMaterial;
    public Material liquidMaterial;

    public Chunk[,] chunksInWorld = new Chunk[World_Settings.worldSizeInChunks, World_Settings.worldSizeInChunks];

    public List<ChunkCoordinate> chunksToInitialize = new List<ChunkCoordinate>();

    //  List and stuff

    private Queue<Queue<VoxelModification>> modifications = new Queue<Queue<VoxelModification>>();

    private List<Chunk> chunksToPopulateVoxelMap = new List<Chunk>();
    private List<Chunk> chunksToUpdateMesh = new List<Chunk>();

    public Queue<Chunk> chunksToDrawLodOne = new Queue<Chunk>();
    public Queue<Chunk> chunksToDrawWater = new Queue<Chunk>();
    public Queue<Chunk> chunksToDrawLodTwo = new Queue<Chunk>();

    //  Threading related

    private bool applyingModifications = false;

    private Thread UpdateChunk_Thread;

    private object populateVoxel_lock = new object();
    private object modficiation_lock = new object();
    public object upadateMeshLock = new object();

    private void Awake()
    {
        World_Generation.GenerateWorld(this);

        GetSpawnPosition();

        //  Starts a new threads for chunk updating, renderdistance checks and voxelmap populating

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

        spawn.transform.position = new Vector3((World_Settings.worldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight, (World_Settings.worldSizeInChunks * VoxelData.chunkWidth) / 2f);
        spawn.transform.position = new Vector3(spawn.transform.position.x, TerrainHeight.GetTerrainHeight(this, spawn.transform.position) + 2, spawn.transform.position.z);
    }

    public void EnqueueNewChunk(int x, int z)
    {
        //  Adds new chunk to chunk array and then creates lod one of chunk

        if (chunksInWorld[x, z] != null)
        {
            lock (populateVoxel_lock)
            {
                chunksToPopulateVoxelMap.Add(chunksInWorld[x, z]);
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
            if (chunksToPopulateVoxelMap.Count > 0)
            {
                PopulateVoxelMap();
            }

            if (!applyingModifications)
            {
                ApplyModifications();
            }

            if (chunksToUpdateMesh.Count > 0)
            {
                UpdateChunkMesh();
            }
        }
    }

    private void PopulateVoxelMap()
    {
        //  Starts a new round of chunk populating when it can
        //  Looks for the first chunk thats not populated, ie not editable

        bool updated = false;
        int index = 0;

        lock (populateVoxel_lock)
        {
            while (!updated && index < chunksToPopulateVoxelMap.Count - 1)
            {
                if (!chunksToPopulateVoxelMap[index].IsEditable)
                {
                    chunksToPopulateVoxelMap[index].PopulateVoxelMap();
                    chunksToPopulateVoxelMap.RemoveAt(index);
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
            lock (modficiation_lock)
            {
                Queue<VoxelModification> modQueue = modifications.Dequeue();

                while (modQueue.Count > 0)
                {
                    VoxelModification vm = modQueue.Dequeue();

                    ChunkCoordinate cc = Voxel_Util.GetChunkCoordinateFromVector3(vm.position);

                    if (Voxel_Util.IsChunkInWorld(cc))
                    {
                        if (chunksInWorld[cc.x, cc.z] == null)
                        {
                            Debug.Log("Error in World/ApplyModifications: Chunk doesn't exist");
                        }

                        else
                        {
                            chunksInWorld[cc.x, cc.z].modifications.Enqueue(vm);

                            if (!chunksToUpdateMesh.Contains(chunksInWorld[cc.x, cc.z]))
                            {
                                chunksToUpdateMesh.Add(chunksInWorld[cc.x, cc.z]);
                            }
                        }
                    }
                }
            }
        }

        applyingModifications = false;
    }

    private void UpdateChunkMesh()
    {
        //  Starts a new round of chunk populating when it can
        //  Looks for the first chunk thats not populated, ie not editable

        bool updated = false;
        int index = 0;

        lock (upadateMeshLock)
        {
            while (!updated && index < chunksToUpdateMesh.Count - 1)
            {   
                if (chunksToUpdateMesh[index].IsEditable)
                {
                    chunksToUpdateMesh[index].UpdateChunkLodOne();
                    chunksToUpdateMesh.RemoveAt(index);
                    updated = true;
                }
                else
                {
                    index++;
                }
            }
        }
    }

    public void EnqueueModification(Queue<VoxelModification> queue)
    {
        lock (modficiation_lock)
        {
            modifications.Enqueue(queue);
        }   
    }

    public void InsertMeshUpdate(Chunk chunk)
    {
        lock (upadateMeshLock)
        {
            chunksToUpdateMesh.Insert(0, chunk);
        }
    }

    public void InsertMeshUpdateAdjacent(Chunk chunk, int x, int y, int z)
    {
        lock (upadateMeshLock)
        {
            chunksToUpdateMesh.Insert(0, chunk);
            chunk.UpdateAdjacentVoxels(x, y, z);
        }
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