using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public class RenderDistance : NetworkBehaviour
{
    public ChunkCoordinate playerChunk;
    private ChunkCoordinate playerLastChunk;

    private World world;

    [SerializeField] private GameObject debugScreen;

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate>();

    private Thread CheckRenderDistance_Thread;

    private bool hasCheckedRenderDistance = true;
    private Vector3 playerPosition;

    private Queue<Chunk> chunksToActivate = new Queue<Chunk>();
    private Queue<Chunk> chunksToDeactivate = new Queue<Chunk>();

    private Queue<ChunkCoordinate> chunksToCreate = new Queue<ChunkCoordinate>();

    private void Start()
    {
        world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();

        CheckRenderDistance_Thread = new Thread(new ThreadStart(ThreadedUpdate_3));
        CheckRenderDistance_Thread.Start();

        playerChunk = world.GetChunkCoordinateFromVector3(transform.position);

        playerPosition = transform.position;
        hasCheckedRenderDistance = false;

    }

    private void Update()
    {
        playerChunk = world.GetChunkCoordinateFromVector3(transform.position);

        if (!playerChunk.Equals(playerLastChunk))
        {
            //  If in new chunk, via check render distance checkrenderdistancethread, uses bool to active
            playerPosition = transform.position;
            hasCheckedRenderDistance = false;
        }

        if(chunksToCreate.Count > 0)
        {
            //  Sends chunks that should be activate to the server via rpcmanager
            ChunkCoordinate cc = chunksToCreate.Dequeue();
            world.NewChunk(cc.x, cc.z);
        }

        if (chunksToActivate.Count > 0)
            ActivateChunks();

        if (chunksToDeactivate.Count > 0)
            DeactivateChunks();
        
        //  Should not be here
        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);
    }

    private void ThreadedUpdate_3()
    {
        //  Infinte while loop on seperate thread, very scary :(
        //  Sleeps loop for a second each time cus we dont need to spam it

        while (true)
        {
            if (!hasCheckedRenderDistance)
            {
                CheckRenderDistance();
            }

            Thread.Sleep(1000);
        }
    }

    private void OnDisable()
    {
        //  Aborts infinite loop when we shut down program just in case

        if (IsOwner) CheckRenderDistance_Thread.Abort();

    }

    private void CheckRenderDistance()
    {
        //  Makes a copy of active chunks to compare in loop, if we find it, ie in render distance, we remove it,
        //  after loop we deactivate (in main thread later, cus thread cant) any remaining chunks in the copy, cus these arent in render distance

        //  If we find a chunk that doesnt exist, we add it to list of chunks to be server rpcd to creation
        //  If not activate, activate (in main thread -||-)
        //  Add all chunks in renderdistance to active chunks

        ChunkCoordinate coord = world.GetChunkCoordinateFromVector3(playerPosition);
        playerLastChunk = playerChunk;

        List<ChunkCoordinate> previoslyActiveChunks = new List<ChunkCoordinate>(activeChunks);

        activeChunks.Clear();

        for (int x = coord.x - VoxelData.RenderDistanceInChunks; x < coord.x + VoxelData.RenderDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.RenderDistanceInChunks; z < coord.z + VoxelData.RenderDistanceInChunks; z++)
            {
                if (world.IsChunkInWorld(new ChunkCoordinate(x, z)))
                {
                    if (world.chunksInWorld[x, z] == null)
                    {
                        chunksToCreate.Enqueue(new ChunkCoordinate(x, z));
                    }

                    else if (!world.chunksInWorld[x, z].IsActive)
                    {
                        chunksToActivate.Enqueue(world.chunksInWorld[x, z]);
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

            chunksToDeactivate.Enqueue(world.chunksInWorld[c.x, c.z]);
        }

        hasCheckedRenderDistance = true;
    }

    private void ActivateChunks()
    {
        while (chunksToActivate.Count > 0)
            chunksToActivate.Dequeue().IsActive = true;
    }

    private void DeactivateChunks()
    {
        while (chunksToDeactivate.Count > 0)
        {
            //  null check, if its null this time, its deactivate next batch, so we just skip for now

            Chunk c = chunksToDeactivate.Dequeue();

            if (c != null) c.IsActive = false;
        }
            
    }
}
