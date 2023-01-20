using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float Get2DPerlinHighResoltion(World world, int resolution, Vector2 pos, float _offset, float scale, int octaves, float persistence, float lacunarity, float noiseCutoff)
    {
        //  Sample perlin noise, gets layered through octaves,
        //  persistance changes amplitude through each octave
        //  lacunarity changes frequency through each octave

        //  creates a curve where amplitude is x and frequency is y

        //  Does this x amount of times, and changes the offset, as to get more detailed results
        //  Because the perlin noise has too few asmple points, so when passed very small numbers, 
        //  A lot of coodrinates get the same result

        float noiseHeight = 0f;
        float offset = _offset + world.randomSeed + 0.1f;

        for (int i = 0; i < resolution; i++)
        {
            float noise = 0f;
            offset += i;
            float amplituide = 1f;
            float frequency = 1f;

            for (int n = 0; n < octaves; n++)
            {
                float sampleX = pos.x / scale * frequency;
                float sampleY = pos.y / scale * frequency;

                float perlinValue = Mathf.PerlinNoise(sampleX + offset, sampleY + offset);

                noise += perlinValue * amplituide;
                amplituide *= persistence;
                frequency *= lacunarity;
            }

            if (noise > noiseCutoff) noise = noiseCutoff;

            noiseHeight += noise;
        }

        return noiseHeight / (float)resolution;
    }

    public static float Get2DPerlin(World world, Vector2 pos, float _offset, float scale, int octaves, float persistence, float lacunarity, float noiseCutoff)
    {
        //  Sample perlin noise, gets layered through octaves,
        //  persistance changes amplitude through each octave
        //  lacunarity changes frequency through each octave

        //  creates a curve where amplitude is x and frequency is y

        float amplituide = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;

        float offset = _offset + world.randomSeed + 0.1f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = pos.x / scale * frequency;
            float sampleY = pos.y / scale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX + offset, sampleY + offset);
            
            noiseHeight += perlinValue * amplituide;
            amplituide *= persistence;
            frequency *= lacunarity;
        }

        if(noiseHeight > noiseCutoff) noiseHeight = 1f;

        return noiseHeight;
    }

    public static float GetFallOffMap(Vector2 pos)
    {
        //  Value approaching 0 from when we cross the map edge value

        float halfWorldSizeInVoxel = (float)VoxelData.WorldSizeInVoxel / 2;
        float mapEdge = halfWorldSizeInVoxel - (VoxelData.mapEdgeSizeInChunks * VoxelData.chunkWidth);

        float x = Mathf.Abs(Mathf.FloorToInt(pos.x) - halfWorldSizeInVoxel);
        float y = Mathf.Abs(Mathf.FloorToInt(pos.y) - halfWorldSizeInVoxel);

        float distance = Mathf.Max(x, y);

        if(distance > mapEdge)
        {
            float fallOffValue = (distance - mapEdge) / (halfWorldSizeInVoxel - mapEdge);

            return 1 - fallOffValue;
        }

        return 1;
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
