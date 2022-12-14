using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private RenderDistance playerRenderDistance;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private EntityChunkParent entityChunkParent;

    private World world;

    private void Start()
    {
        world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();
    }

    private void Update()
    {
        //  Waits untill entire spawnchunk has been loaded

        if (world.spawnedChunks >= (VoxelData.RenderDistanceInChunks * 2) * (VoxelData.RenderDistanceInChunks * 2))
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        //  Sets players postion at spawn and enables certain scripts that shouldnt be on while player hasnt spawned
        //  Deactivates itself

        playerMovement.SetPlayerPositionAtSpawnPosition();

        playerRenderDistance.enabled = true;
        entityChunkParent.enabled = true;

        this.enabled = false;
    }
}
