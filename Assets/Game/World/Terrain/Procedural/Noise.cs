using System.Collections;
using System.Collections.Generic;
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

    public static bool Get3DPerlinCave(World world, int terrainHieght, Vector3 position)
    {
        if (position.y > terrainHieght) return false;

        if (Get3DPerlinNoodle(world, position, 0.48f, 0.52f))
        {
            return true;
        }

        else if (Get3DPerlinHell(world, position, 0.8f))
        {
            return true;
        }

        else if (Get3DPerlinCoolCave(world, position, 0.7f))
        {
            return true;
        }

        return false;
    }

    public static bool Get3DPerlinCoolCave(World world, Vector3 position, float _treshold)
    {
        //  Returns 3d noise base of mathimatichal mathimeaticds
        float yPos = position.y;
        yPos += 16f * (1 - (Get2DPerlin(world, new Vector2(position.x, position.z), 500f, 50f, 1, 0.5f, 2f, 10f) * 2f));

        if (yPos > 256f) return false;

        float noise = Get3DPerlin(world, position, 1000f, 100f, 3, 0.5f, 2f);
        float treshold = yPos / VoxelData.chunkHeight;

        if (yPos <= 160f)
        {
            treshold += 1f - ((yPos - 128f) / 32f);
        }

        else if (yPos >= 224f)
        {
            treshold += (yPos - 224f) / 32f;
        }

        if (treshold + _treshold < noise)
            return true;

        else
            return false;
    }

    public static bool Get3DPerlinHell(World world, Vector3 position, float _treshold)
    {
        //  Returns 3d noise base of mathimatichal mathimeaticds
        float yPos = position.y;
        yPos += 16f * (1 - (Get2DPerlin(world, new Vector2(position.x, position.z), 500f, 50f, 1, 0.5f, 2f, 10f) * 2f));

        if (yPos > 128f) return false;

        float noise = Get3DPerlin(world, position, 2000f, 100f, 3, 0.5f, 2f);
        float treshold = yPos / VoxelData.chunkHeight;

        if (yPos <= 32)
        {
            treshold += 1f - (yPos / 32f);
        }

        else if (yPos >= 96)
        {
            treshold += (yPos - 96f) / 32f;
        }

        if (treshold + _treshold < noise)
            return true;

        else
            return false;
    }

    public static bool Get3DPerlinNoodle(World world, Vector3 position, float threshold1, float threshold2)
    {
        //  Returns 3d noise base of mathimatichal mathimeaticds

        bool ringOne = false;
        bool ringTwo = false;

        float yPos = position.y;

        //  Ring one

        float noise1 = Get3DPerlinNoodle(world, position, 3000f, 200f, 1, 0.5f, 2f);

        if (threshold1 < noise1 && noise1 < threshold2)
        {
            ringOne = true;
        }

        //  Ring two

        float noise2 = Get3DPerlinNoodle(world, position, 4000f, 200f, 1, 0.2f, 2f);

        if (threshold1 < noise2 && noise2 < threshold2)
        {
            ringTwo = true;
        }

        //  If overlap, return true

        if (ringOne && ringTwo)
            return true;
        else
            return false;

    }

    public static float Get3DPerlinNoodle(World world, Vector3 position, float _offset, float scale, int octaves, float persistence, float lacunarity)
    {
        float amplituide = 1f;
        float frequency = 1f;
        float noise = 0f;

        float offset = _offset + world.randomSeed + 0.1f;

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

            float perlinValue = (AB + BC + AC + CA + BA + CB + CA) / 6f;

            noise += perlinValue * amplituide;
            amplituide *= persistence;
            frequency *= lacunarity;
        }

        return noise;
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

    public static float Get3DPerlin(World world, Vector3 position, float _offset, float scale, int octaves, float persistence, float lacunarity)
    {
        float amplituide = 1f;
        float frequency = 1f;
        float noise = 0f;

        float offset = _offset + world.randomSeed + 0.1f;

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

            float perlinValue = (AB + BC + AC + CA + BA + CB + CA) / 6f;

            noise += perlinValue * amplituide;
            amplituide *= persistence;
            frequency *= lacunarity;
        }

        return noise;
    }

    #endregion
}
