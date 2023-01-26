using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    #region world

    public static int Get2DPerlinWorld(World world, Vector2 pos, float _offset, float scale, int octaves, float persistence, float lacunarity)
    {
        float noise = GetWorldBaseHeight(world, pos, _offset, scale, octaves, persistence, lacunarity);

        noise *= GetWorldHeightMultiplier(world, pos, _offset, scale);

        float noiseHeight = (VoxelData.waterHeight + (world.worldHeight * noise)) * GetFallOffMap(pos);

        noiseHeight = Mathf.Clamp(noiseHeight, 0, VoxelData.chunkHeight);

        return Mathf.FloorToInt(noiseHeight);
    }

    public static float GetWorldBaseHeight(World world, Vector2 pos, float _offset, float scale, int octaves, float persistence, float lacunarity)
    {
        float amplituide = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;

        float offset = _offset + world.randomSeed + 0.1f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = pos.x / scale * frequency;
            float sampleY = pos.y / scale * frequency;

            offset += scale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX + offset, sampleY + offset);

            noiseHeight += perlinValue;
            amplituide *= persistence;
            frequency *= lacunarity;
        }

        return noiseHeight;
    }

    public static float GetWorldHeightMultiplier(World world, Vector2 pos, float _offset, float scale)
    {
        float offset = _offset + world.randomSeed + 0.1f;

        scale *= 2;

        float sampleX = pos.x / scale;
        float sampleY = pos.y / scale;

        float heightMultiplier = Mathf.PerlinNoise(sampleX + offset, sampleY + offset);

        heightMultiplier = 0.4f + (Mathf.Clamp(heightMultiplier, 0, 1) * 2f);

        heightMultiplier = Mathf.Pow(heightMultiplier, heightMultiplier);

        return heightMultiplier;
    }

    #endregion

    #region humidity

    public static float Get2DPerlinHumidity(World world, Vector2 pos, float _offset, float scale, int octaves, float persistence, float lacunarity, float noiseCutoff)
    {
        float humidity = Get2DPerlin(world, pos, _offset, scale, octaves, persistence, lacunarity, noiseCutoff);

        humidity *= 1 - GetFallOffMap(pos);

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
            float sampleX = pos.x / scale * frequency;
            float sampleY = pos.y / scale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX + offset, sampleY + offset);

            noiseHeight += perlinValue * amplituide;
            amplituide *= persistence;
            frequency *= lacunarity;
        }

        if (noiseHeight > noiseCutoff) noiseHeight = 1f;

        return noiseHeight;
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

        if ((AB + BC + AC + CA + BA + CB + CA) / 6f > threshold)
            return true;
        else
            return false;
    }

    #endregion
}
