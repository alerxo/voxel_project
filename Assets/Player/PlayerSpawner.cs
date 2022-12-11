using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private bool playerSpawned = false;

    private World world;

    private void Start()
    {
        world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();
    }

    private void Update()
    {
        if(world.spawnLoaded && !playerSpawned)
        {
            player.SetActive(true);
            playerSpawned = true;
            this.enabled = false;
        }
    }
}
