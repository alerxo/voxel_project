using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownTest : MonoBehaviour
{
    public SubBiome voidBiome;
    public Town testTown;
    public int chunkAmount;

    private int yPos = 100;

    private void Start()
    {
        Debug.Log("Generating world");

        World world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();

        int start = (World_Settings.worldSizeInChunks / 2) - chunkAmount;
        int end = (World_Settings.worldSizeInChunks / 2) + chunkAmount;

        for (int x = start; x < end; x++)
        {
            for (int z = start; z < end; z++)
            {
                world.EnqueueNewChunk(x, z);
                world.chunksInWorld[x, z].IsActive = true;
            }
        }

        Debug.Log("Generating town");

        Vector3 townPosition = world.chunksInWorld[World_Settings.worldSizeInChunks / 2, World_Settings.worldSizeInChunks / 2].position;
        townPosition.y = yPos;

        TownData townData = new TownData(world, testTown, townPosition);
        GenerateTown.GenerateTownData(townData);

        Debug.Log("Done");
    }
}
