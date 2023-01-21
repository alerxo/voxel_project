using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private RenderDistance playerRenderDistance;
    [SerializeField] private PlayerMovement playerMovement;

    private GameObject spawn;

    private void Start()
    {
        spawn = GameObject.FindGameObjectWithTag("SpawnObj");
    }

    private void Update()
    {
        //  Waits untill entire spawnchunk has been loaded

        if (spawn.transform.position != new Vector3(0, 0, 0))
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        //  Sets players postion at spawn and enables certain scripts that shouldnt be on while player hasnt spawned
        //  Deactivates itself

        playerMovement.SetPlayerPositionAtSpawnPosition();

        if (IsOwner) playerRenderDistance.enabled = true;

        enabled = false;
    }
}
