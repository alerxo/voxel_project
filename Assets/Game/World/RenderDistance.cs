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

    private GameObject worldObj;
    private World world;

    [SerializeField] private RpcHandler rpcHandler;

    [SerializeField] private GameObject debugScreen;

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate>();

    private Thread CheckRenderDistance_Thread;

    private bool hasCheckedRenderDistance = true;
    private Vector3 playerPosition;

    private Queue<Chunk> chunksToActivate = new Queue<Chunk>();
    private Queue<Chunk> chunksToDeactivate = new Queue<Chunk>();

    private Queue<ChunkCoordinate> chunksToServerRpc = new Queue<ChunkCoordinate>();

    private void Start()
    {
        worldObj = GameObject.FindGameObjectWithTag("WorldObj");
        world = worldObj.GetComponent<World>();

        if (!IsOwner) return;

        CheckRenderDistance_Thread = new Thread(new ThreadStart(ThreadedUpdate_3));
        CheckRenderDistance_Thread.Start();

        playerChunk = world.GetChunkCoordinateFromVector3(transform.position);

        playerPosition = transform.position;
        hasCheckedRenderDistance = false;

    }

    private void Update()
    {
        if (!IsOwner) return;

        playerChunk = world.GetChunkCoordinateFromVector3(transform.position);

        if (!playerChunk.Equals(playerLastChunk))
        {
            playerPosition = transform.position;
            hasCheckedRenderDistance = false;
        }

        if(chunksToServerRpc.Count > 0)
        {
            ChunkCoordinate cc = chunksToServerRpc.Dequeue();

            rpcHandler.NewChunkServerRpc(cc.x, cc.z);
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

        CheckRenderDistance_Thread.Abort();
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

        List<ChunkCoordinate> previoslyActiveChunks = new List<ChunkCoordinate>(activeChunks);

        activeChunks.Clear();

        for (int x = coord.x - VoxelData.RenderDistanceInChunks; x < coord.x + VoxelData.RenderDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.RenderDistanceInChunks; z < coord.z + VoxelData.RenderDistanceInChunks; z++)
            {
                if (world.IsChunkInWorld(new ChunkCoordinate(x, z)))
                {
                    if (world.chunks[x, z] == null)
                    {
                        chunksToServerRpc.Enqueue(new ChunkCoordinate(x, z));
                    }

                    else if (!world.chunks[x, z].isActive)
                    {
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
        }

        hasCheckedRenderDistance = true;
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
}
