using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class World_Settings
{
    // Player settings

    public static readonly int RenderDistanceInChunks = 8;

    //  World Settings

    private static int seed = 1;

    public static int randomSeed { get; private set; }

    //  Noise

    public static int worldHeight = 32;
    public static float noiseScale = 64;
    public static float multiplierNoiseScale = 128;
    public static int octaves = 3;
    public static float persistence = 0.3f;
    public static float lacunarity = 1.3f;

    //  World data  

    public static int riverSourceHeightLimit = 300;
    public static float riverSourceHumidityLimit = 0.6f;

    public static readonly int mapEdgeSizeInChunks = 32; //  Edge of world, which is forced ocean
    public static readonly int landSizeInChunks = 64; //    Land, where continents are found
    public static readonly int waterHeight = 256;

    public static int worldSizeInChunks
    {
        get { return (mapEdgeSizeInChunks * 2) + landSizeInChunks; }
    }

    public static int WorldSizeInVoxel
    {
        get { return worldSizeInChunks * VoxelData.chunkWidth; }
    }

    public static void ScrambleSeed()
    {
        Random.InitState(seed);
        randomSeed = Random.Range(0, 1000000);
    }
}
