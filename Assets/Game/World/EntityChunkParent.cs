using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EntityChunkParent : NetworkBehaviour
{
    private World world;

    private ChunkCoordinate playerChunk;
    private ChunkCoordinate playerLastChunk;

    private Chunk parentChunk;
    private Chunk lastParentChunk;

    private void Start()
    {
        world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();
    }

    private void Update()
    {
        //  If we are not in a loaded chunk, dont run 

        if (world.GetChunkFromVector3(transform.position) == null) return;

        playerChunk = world.GetChunkCoordinateFromVector3(transform.position);

        if (!playerChunk.Equals(playerLastChunk))
        {
            //  When we are in new chunk change parent chunk

            playerLastChunk = playerChunk;
            lastParentChunk = parentChunk;
            parentChunk = world.GetChunkFromVector3(transform.position);

            if (lastParentChunk == null)
            {
                AddParent(parentChunk);
            }

            else
            {
                ChangeParent(lastParentChunk, parentChunk);
            }
        }

        parentChunk = world.GetChunkFromVector3(transform.position);

        if (!IsOwner && (gameObject.activeSelf != parentChunk.IsActive))
        {
            //  For the non owners, update entity acitve state from parent state

            gameObject.SetActive(parentChunk.IsActive);
        }
    }

    private void AddParent(Chunk chunk)
    {
        chunk.childObjects.Add(gameObject);
    }

    private void RemoveParent(Chunk chunk)
    {
        chunk.childObjects.Remove(gameObject);
    }

    private void ChangeParent(Chunk oldChunk, Chunk newChunk)
    {
        RemoveParent(oldChunk);
        AddParent(newChunk);
    }
}
