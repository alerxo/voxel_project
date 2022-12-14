using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Unity.Netcode;

public class World : NetworkBehaviour
{
    [SerializeField] private int seed;
    public Biome biome;

    [SerializeField] private GameObject spawn;
    public int spawnedChunks { get; private set; }

    private NetworkManager networkManager;

    //  Voxel related

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate>();

    public Material material;
    public Material transparentMaterial;

    public Block[] blocks;

    public Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    public List<ChunkCoordinate> chunksToInitialize = new List<ChunkCoordinate>();

    //  Threading related

    private Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public List<Chunk> chunksToPopulate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    private bool applyingModifications = false;

    private Thread ThreadedUpdate;
    public object ThreadedUpdate_Lock = new object();

    private void Start()
    {
        //  Starts a new threads for chunk updating, renderdistance checks and voxelmap populating

        networkManager = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<NetworkManager>();

        Random.InitState(seed);

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
            if(GetVoxelBlockType(new Vector3(spawn.transform.position.x, i, spawn.transform.position.z)) == 1)
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

        ChunkCoordinate newChunk = new ChunkCoordinate(x, z);
        chunks[x, z] = new Chunk(newChunk, this);
        chunksToInitialize.Add(newChunk);
    }

    private void InitializeChunk()
    {
        //  Initializes first chunk in list and removes,
        //  adds to spawned chunks so other scripts can caluclate when to do something

        ChunkCoordinate cc = chunksToInitialize[0];
        chunksToInitialize.RemoveAt(0);
        chunks[cc.x, cc.z].Init();

        spawnedChunks++;
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

            Thread.Sleep(10);
        }
    }

    private void OnDisable()
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

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].IsEditable)
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

    public bool IsChunkInWorld(ChunkCoordinate coord)
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