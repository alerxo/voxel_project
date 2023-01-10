using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float Get2DPerlin(World world, Vector2 pos, float _offset, float scale, int octaves, float persistence, float lacunarity, float noiseCutoff)
    {
        //  Sample perlin noise, gets layered through octaves,
        //  persistance changes amplitude through each octave
        //  lacunarity changes frequency through each octave

        //  creates a curve where amplitude is x and frequency is y

        float amplituide = 1;
        float frequency = 1;
        float noiseHeight = 0;

        float offset = _offset + world.randomSeed;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = pos.x / scale * frequency;
            float sampleY = pos.y / scale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX + offset, sampleY + offset);
            
            noiseHeight += perlinValue * amplituide;
            amplituide *= persistence;
            frequency *= lacunarity;
        }

        if(noiseHeight > noiseCutoff) noiseHeight = 1;

        return noiseHeight;
    }

    public static float GetFallOffMap(Vector2 pos)
    {
        //  Value aproaching 0 from spawn
        //  Distance from spawn divided by half world, so the further we are, the larger the number
        //  1 minus this because we want opposite

        float halfWorldSizeInVoxel = VoxelData.WorldSizeInVoxel / 2;

        float x = Mathf.Abs(Mathf.FloorToInt(pos.x) - halfWorldSizeInVoxel);
        float y = Mathf.Abs(Mathf.FloorToInt(pos.y) - halfWorldSizeInVoxel);

        if (x > y) return 1 - (x / halfWorldSizeInVoxel);
        else return 1 - (y / halfWorldSizeInVoxel);
    }

    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold)
    {
        //  Returns 3d noise base of mathimatichal mathimeaticds

        float x = (position.x + offset + 0.1f) * scale;
        float y = (position.y + offset + 0.1f) * scale;
        float z = (position.z + offset + 0.1f) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        if((AB + BC + AC + CA + BA + CB + CA) / 6f > threshold)
            return true;
        else
            return false;
    }
}
