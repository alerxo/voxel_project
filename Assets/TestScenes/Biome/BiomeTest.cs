using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeTest : MonoBehaviour
{
    public SubBiome testBiome;
    public int chunkAmount;

    private void Start()
    {
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
    }
}
