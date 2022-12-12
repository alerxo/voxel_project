using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject player;

    private int chunksSpawned;

    private void OnEnable()
    {
        EventManager.NewChunk += EventManager_NewChunk;
    }

    private void OnDisable()
    {
        EventManager.NewChunk -= EventManager_NewChunk;
    }

    private void EventManager_NewChunk()
    {
        chunksSpawned += 1;

        if(chunksSpawned > (VoxelData.RenderDistanceInChunks * 2) *  (VoxelData.RenderDistanceInChunks * 2))
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        //  Waits for spawn to load and spawns player and deactivates itself

        player.SetActive(true);
        this.enabled = false;
    }
}
