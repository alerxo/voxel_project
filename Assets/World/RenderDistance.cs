using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class RenderDistance : MonoBehaviour
{
    public ChunkCoordinate playerChunk;
    private ChunkCoordinate playerLastChunk;

    private GameObject worldObj;
    private World world;

    [SerializeField] private GameObject debugScreen;

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate>();

    private Thread Thread_3;
    public object Thrad_3Lock = new object();

    private bool hasCheckedRenderDistance = false;
    private Vector3 playerPosition;

    private Queue<Chunk> chunksToActivate = new Queue<Chunk>();
    private Queue<Chunk> chunksToDeactivate = new Queue<Chunk>();

    private void Start()
    {
        worldObj = GameObject.FindGameObjectWithTag("WorldObj");
        world = worldObj.GetComponent<World>();

        // Thread_3 = new Thread(new ThreadStart(ThreadedUpdate_3));
        // Thread_3.Start();

        playerChunk = world.GetChunkCoordinateFromVector3(transform.position);

        playerPosition = transform.position;
        hasCheckedRenderDistance = false;

        CheckRenderDistance();
    }

    private void Update()
    {
        playerChunk = world.GetChunkCoordinateFromVector3(transform.position);

        if (!playerChunk.Equals(playerLastChunk))
        {
            playerPosition = transform.position;
            hasCheckedRenderDistance = false;

            CheckRenderDistance();
        }

        if (chunksToActivate.Count > 0)
            ActivateChunks();

        if (chunksToDeactivate.Count > 0)
            DeactivateChunks();


        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);
    }

    private void ThreadedUpdate_3()
    {
        //  Infinte while loop on seperate thread, very scary :(

        while (true)
        {
            if (!hasCheckedRenderDistance)
                CheckRenderDistance();
        }
    }

    private void OnDisable()
    {
        //  Aborts infinite loop when we shut down program just in case

        Thread_3.Abort();
    }

    private void CheckRenderDistance()
    {
        //  Checks if there are any chunks in render distance that should exist but dont
        //  If so, generate
        //  Else if it exists but not active, tell main thread to activate
        //  Then check if any old chunks are outside of render distance
        //  If so, tell main thread to deactivate

        ChunkCoordinate coord = world.GetChunkCoordinateFromVector3(playerPosition);
        playerLastChunk = playerChunk;

        Debug.Log(playerChunk.x.ToString() + playerChunk.z.ToString());

        List<ChunkCoordinate> previoslyActiveChunks = new List<ChunkCoordinate>(activeChunks);

        activeChunks.Clear();

        for (int x = coord.x - VoxelData.RenderDistanceInChunks; x < coord.x + VoxelData.RenderDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.RenderDistanceInChunks; z < coord.z + VoxelData.RenderDistanceInChunks; z++)
            {
                if (world.IsChunkInWorld(new ChunkCoordinate(x, z)))
                {
                    Debug.Log(x.ToString() + z.ToString() + "testing");

                    if (world.chunks[x, z] == null)
                    {
                        //  Debug.Log(x.ToString() + z.ToString() + "dont exist");

                        world.chunks[x, z] = new Chunk(new ChunkCoordinate(x, z), world);
                        world.chunksToInitialize.Add(new ChunkCoordinate(x, z));
                    }

                    else if (!world.chunks[x, z].isActive)
                    {
                        //  Debug.Log(x.ToString() + z.ToString() + "activating");
                        chunksToActivate.Enqueue(world.chunks[x, z]);
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

            chunksToDeactivate.Enqueue(world.chunks[c.x, c.z]);

            Debug.Log(c.x.ToString() + c.z.ToString() + "removing");
        }

        hasCheckedRenderDistance = true;
    }

    private void ActivateChunks()
    {
        while (chunksToActivate.Count > 0)
        {
            // Debug.Log(chunksToActivate.Peek().ToString());
            chunksToActivate.Dequeue().isActive = true;
        }
            
    }

    private void DeactivateChunks()
    {
        while (chunksToDeactivate.Count > 0)
            chunksToDeactivate.Dequeue().isActive = false;
    }
}
