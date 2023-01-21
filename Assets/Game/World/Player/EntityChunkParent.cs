using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EntityChunkParent : NetworkBehaviour
{
    [SerializeField] private GameObject entity;

    private World world;

    private ChunkCoordinate playerChunkCoordinate;
    private ChunkCoordinate playerLastChunkCoordinate;

    private Chunk parentChunk;
    private Chunk lastParentChunk;

    private void Start()
    {
        world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();
    }

    private void Update()
    {
        //  When entity moves into new chunk, we call server rpc, to call a client rpc
        //  This client rpc updates the parentchunk for this entity on all clients

        if (!IsOwner) return;

        playerChunkCoordinate = Voxel_Util.GetChunkCoordinateFromVector3(entity.transform.position);

        if (!playerChunkCoordinate.Equals(playerLastChunkCoordinate))
        {
            playerLastChunkCoordinate = playerChunkCoordinate;
            NewParentServerRpc(playerChunkCoordinate.x, playerChunkCoordinate.z);
        }
    }

    [ServerRpc]
    private void NewParentServerRpc(int x, int z)
    {
        NewParentClientRpc(x, z);
    }

    [ClientRpc]
    private void NewParentClientRpc(int x, int z)
    {
        if (IsOwner) return;

        Chunk tempChunk = world.chunksInWorld[x, z];

        if (tempChunk == null)
        {
            //  If entity not in a existing chunk, remove parent and set inactive

            RemoveParent(parentChunk);
            entity.SetActive(false);
            return;
        }

        else
        {
            //  If entity in an existing chunk change the parent and update

            lastParentChunk = parentChunk;
            parentChunk = tempChunk;

            RemoveParent(lastParentChunk);
            AddParent(parentChunk);

            if (entity.activeSelf != parentChunk.IsActive)
            {
                //  Updates the state of entity based on parent

                entity.SetActive(parentChunk.IsActive);
            }
        }
    }

    private void AddParent(Chunk chunk)
    {
        chunk?.childObjects.Add(entity);
    }

    private void RemoveParent(Chunk chunk)
    {
        chunk?.childObjects.Remove(entity);
    }
}
