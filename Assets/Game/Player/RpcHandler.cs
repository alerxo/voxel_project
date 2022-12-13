using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public class RpcHandler : NetworkBehaviour
{
    private World world;

    private void Start()
    {
        world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();
    }

    [ServerRpc]
    public void NewChunkServerRpc(int x, int z)
    {
        world.NewChunk(x, z);
    }
}
