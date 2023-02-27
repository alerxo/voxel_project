using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownTest : MonoBehaviour
{
    public Town testTown;

    private void Start()
    {
        World world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();

        Debug.Log("Generating town");

        Vector3 townPosition = world.chunksInWorld[World_Settings.worldSizeInChunks / 2, World_Settings.worldSizeInChunks / 2].position;

        TownData townData = new TownData(world, testTown, townPosition);
        GenerateTown.GenerateTownData(townData);

        Debug.Log("Done");
    }
}
