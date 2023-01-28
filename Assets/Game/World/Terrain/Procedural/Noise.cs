using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class Noise
{
    #region world

    public static int Get2DPerlinWorld(World world, Vector2 pos)
    {
        float noise = GetWorldBaseHeight(world, pos, world.noiseScale, world.octaves, world.persistence, world.lacunarity);

        noise *= GetWorldHeightMultiplier(world, pos, world.multiplierNoiseScale);

        float noiseHeight = (VoxelData.waterHeight + (world.worldHeight * noise)) * GetFallOffMap(pos);

        noiseHeight = Mathf.Clamp(noiseHeight, 0, VoxelData.chunkHeight);

        return Mathf.FloorToInt(noiseHeight);
    }

    public static float GetWorldBaseHeight(World world, Vector2 pos, float scale, int octaves, float persistence, float lacunarity)
    {
        float amplituide = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;

        float offset = world.randomSeed + 0.1f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (pos.x + 1) / scale * frequency;
            float sampleY = (pos.y + 1) / scale * frequency;

            offset += 1;

            float perlinValue = Mathf.PerlinNoise(sampleX + offset, sampleY + offset);

            noiseHeight += perlinValue;
            amplituide *= persistence;
            frequency *= lacunarity;
        }

        return noiseHeight;
    }

    public static float GetWorldHeightMultiplier(World world, Vector2 pos, float scale)
    {
        float offset = world.randomSeed + 0.1f;

        float sampleX = ((pos.x + 1) / scale);
        float sampleY = ((pos.y + 1) / scale);

        float amplituide = 1f;
        float heightMultiplier = 0;

        for (int i = 0; i < 4; i++)
        {
            offset += sampleX / sampleY;

            float perlinValue = Mathf.Clamp01(Mathf.PerlinNoise(sampleX + offset, sampleY + offset));

            heightMultiplier += perlinValue * amplituide;

            amplituide *= perlinValue;
        }

        heightMultiplier = Mathf.Pow(heightMultiplier, heightMultiplier);

        return heightMultiplier * Mathf.Clamp01(Mathf.PerlinNoise(sampleX, sampleY));
    }

    #endregion

    #region cave

    public static bool Get3DPerlinCave(int terrainHieght, Vector3 position)
    {
        float yPos = position.y;

        float treshold = (yPos / VoxelData.chunkHeight) + 1f;

        float level = yPos / VoxelData.waterHeight;

        if (yPos <= 32)
        {
            treshold += 1f - (yPos / 32f);
        }

        if (Get3DPerlinCaveLevelTwo(position, 200f, 0.49f, 0.51f))
        {
            return true;
        }

        else if(level < 0.5f)
        {
            if(yPos >= 96)
            {
                treshold += (yPos - 96f) / 32f;
            }

            if (Get3DPerlinCaveLevelOne(position, 100f, 4, 0.5f, 2f, treshold))
            {
                return true;
            }
        }



        return false;
    }

    public static bool Get3DPerlinCaveLevelOne(Vector3 position, float scale, int octaves, float persistence, float lacunarity, float threshold)
    {
        //  Returns 3d noise base of mathimatichal mathimeaticds

        float amplituide = 1f;
        float frequency = 1f;

        float noise = 0;
        float offset = 100;

        for (int i = 0; i < octaves; i++)
        {
            float x = (position.x + offset + 0.1f) / scale * frequency;
            float y = (position.y + offset + 0.1f) / scale * frequency;
            float z = (position.z + offset + 0.1f) / scale * frequency;

            float AB = Mathf.PerlinNoise(x, y);
            float BC = Mathf.PerlinNoise(y, z);
            float AC = Mathf.PerlinNoise(x, z);
            float BA = Mathf.PerlinNoise(y, x);
            float CB = Mathf.PerlinNoise(z, y);
            float CA = Mathf.PerlinNoise(z, x);

            noise += ((AB + BC + AC + CA + BA + CB + CA) / 6f) * amplituide;

            amplituide *= persistence;
            frequency *= lacunarity;
        }

        if (noise > threshold)
            return true;
        else
            return false;
    }

    public static bool Get3DPerlinCaveLevelTwo(Vector3 position, float scale, float threshold1, float threshold2)
    {
        //  Returns 3d noise base of mathimatichal mathimeaticds

        float offset = 200;

        float x = (position.x + offset + 0.1f) / scale;
        float y = (position.y + offset + 0.1f) / scale;
        float z = (position.z + offset + 0.1f) / scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        float noise = (AB + BC + AC + CA + BA + CB + CA) / 6f;

        if (threshold1 < noise && noise < threshold2)
            return true;
        else
            return false;
    }

    #endregion

    #region humidity

    public static float Get2DPerlinHumidity(World world, Vector2 pos, float _offset, float scale, int octaves, float persistence, float lacunarity, float noiseCutoff)
    {
        float humidity = Get2DPerlin(world, pos, _offset, scale, octaves, persistence, lacunarity, noiseCutoff);

        humidity = Mathf.Clamp01(humidity);

        return humidity;
    }

    #endregion

    #region temperature

    public static float GetTemperature(Vector2 pos)
    {
        //  Gets the distance from equator
        //  Then assign temperature depending on distance

        float longitude = pos.y;

        float distance = Mathf.Abs((VoxelData.WorldSizeInVoxel / 2) - longitude);

        float temperature = distance / (VoxelData.WorldSizeInVoxel / 2);

        temperature = Mathf.Clamp01(temperature);   

        return 1 - temperature;
    }

    #endregion

    #region util    

    public static float GetFallOffMap(Vector2 pos)
    {
        //  Value approaching 0 from when we cross the map edge value

        float halfWorldSizeInVoxel = (float)VoxelData.WorldSizeInVoxel / 2;
        float mapEdge = halfWorldSizeInVoxel - (VoxelData.mapEdgeSizeInChunks * VoxelData.chunkWidth);

        float x = Mathf.Abs(Mathf.FloorToInt(pos.x) - halfWorldSizeInVoxel);
        float y = Mathf.Abs(Mathf.FloorToInt(pos.y) - halfWorldSizeInVoxel);

        float distance = Mathf.Max(x, y);

        if (distance > mapEdge)
        {
            float fallOffValue = (distance - mapEdge) / (halfWorldSizeInVoxel - mapEdge);

            return 1 - fallOffValue;
        }
        return 1;
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
            float sampleX = (pos.x + 1) / scale * frequency;
            float sampleY = (pos.y + 1) / scale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX + offset, sampleY + offset);

            noiseHeight += perlinValue * amplituide;
            amplituide *= persistence;
            frequency *= lacunarity;
        }

        if (noiseHeight > noiseCutoff) noiseHeight = 1f;

        return noiseHeight;
    }



    #endregion
}
