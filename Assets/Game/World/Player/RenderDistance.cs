using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class RenderDistance : MonoBehaviour
{
    public ChunkCoordinate playerChunk;
    private ChunkCoordinate playerLastChunk;

    private World world;

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate>();

    private Thread CheckRenderDistance_Thread;

    private Vector3 playerPosition;

    private Queue<Chunk> chunksToActivate = new Queue<Chunk>();
    private Queue<Chunk> chunksToDeactivate = new Queue<Chunk>();
    private Queue<ChunkCoordinate> chunksToEnqueue = new Queue<ChunkCoordinate>();

    private void Start()
    {
        world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();

        CheckRenderDistance_Thread = new Thread(new ThreadStart(ThreadedUpdate));
        CheckRenderDistance_Thread.Start();

        playerChunk = Voxel_Util.GetChunkCoordinateFromVector3(transform.position);

        playerPosition = transform.position;
    }

    private void Update()
    {
        playerChunk = Voxel_Util.GetChunkCoordinateFromVector3(transform.position);

        if (!playerChunk.Equals(playerLastChunk) || !gameObject.GetComponent<PlayerMovement>().CanMove())
        {
            //  If in new chunk, reset sweep distance
            playerPosition = transform.position;
        }

        if (chunksToEnqueue.Count > 0)
        {
            //  Sends chunks that should be activate to world

            ChunkCoordinate cc = chunksToEnqueue.Dequeue();
            world.EnqueueNewChunk(cc.x, cc.z);
        }

        if (chunksToActivate.Count > 0)
        {
            ActivateChunks();
        }

        if (chunksToDeactivate.Count > 0)
        {
            DeactivateChunks();
        }
    }

    private void ThreadedUpdate()
    {
        //  Infinte while loop on seperate thread, very scary :(
        //  Sleeps loop for a second each time cus we dont need to spam it

        while (true)
        {
            CheckRenderDistance();

            Thread.Sleep(1000);
        }
    }

    private void OnDisable()
    {
        //  Aborts infinite loop when we shut down program just in case

        CheckRenderDistance_Thread?.Abort();
    }

    private void OnDestroy()
    {
        //  Aborts infinite loop when we shut down program just in case

        CheckRenderDistance_Thread?.Abort();
    }

    private void CheckRenderDistance()
    {
        //  Makes a copy of active chunks to compare in loop, if we find it, ie in render distance, we remove it,
        //  after loop we deactivate (in main thread later, cus thread cant) any remaining chunks in the copy, cus these arent in render distance

        //  If we find a chunk that doesnt exist, we add it to list of chunks to be sent to world
        //  If not activate, activate (in main thread -||-)
        //  Add all chunks in renderdistance to active chunks

        ChunkCoordinate coord = Voxel_Util.GetChunkCoordinateFromVector3(playerPosition);
        playerLastChunk = playerChunk;

        int renderDistance = VoxelData.RenderDistanceInChunks;

        ChunkSweep(coord, renderDistance);
    }

    private void ChunkSweep(ChunkCoordinate coord, int distance)
    {
        List<ChunkCoordinate> previoslyActiveChunks = new List<ChunkCoordinate>(activeChunks);
        activeChunks.Clear();

        for (int x = coord.x - distance; x < coord.x + distance; x++)
        {
            for (int z = coord.z - distance; z < coord.z + distance; z++)
            {
                if (Voxel_Util.IsChunkInWorld(new ChunkCoordinate(x, z)))
                {
                    if (!world.chunksInWorld[x, z].hasBeenEnqueued)
                    {
                        world.chunksInWorld[x, z].hasBeenEnqueued = true;
                        chunksToEnqueue.Enqueue(new ChunkCoordinate(x, z));
                    }

                    if (!world.chunksInWorld[x, z].IsActive)
                    {
                        chunksToActivate.Enqueue(world.chunksInWorld[x, z]);
                    }

                    activeChunks.Add(new ChunkCoordinate(x, z));

                    for (int i = 0; i < previoslyActiveChunks.Count; i++)
                    {
                        if (previoslyActiveChunks[i].Equals(new ChunkCoordinate(x, z)))
                        {
                            previoslyActiveChunks.RemoveAt(i);
                        }
                    }
                }
            }
        }

        foreach (ChunkCoordinate c in previoslyActiveChunks)
        {
            activeChunks.Remove(new ChunkCoordinate(c.x, c.z));

            chunksToDeactivate.Enqueue(world.chunksInWorld[c.x, c.z]);
        }
    }

    private void ActivateChunks()
    {
        while (chunksToActivate.Count > 0)
        {
            chunksToActivate.Dequeue().IsActive = true;
        }
    }

    private void DeactivateChunks()
    {
        while (chunksToDeactivate.Count > 0)
        {
            //  null check, if its null this time, its deactivate next batch, so we just skip for now

            Chunk chunk = chunksToDeactivate.Dequeue();

            if (chunk != null) chunk.IsActive = false;
        }

    }
}