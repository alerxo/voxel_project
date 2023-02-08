using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    #region world

    public static int Get2DPerlinWorld(World world, Vector2 pos)
    {
        //  Gets base worldheight as an interger, by layering different types of noise
        //  First gets an value from a perlin noise,
        //  Then amplifies that value depending on a different perlin noise
        //  Then adds water height to this value, as its the base height
        //  Then multiples this by a fall off value, which approaches 0 from the start of the world edge, resulting in large bodies of water surrounding the continent

        float noise = GetWorldBaseHeight(world, pos, world.noiseScale, world.octaves, world.persistence, world.lacunarity);

        noise *= GetWorldHeightMultiplier(world, pos, world.multiplierNoiseScale);

        float noiseHeight = (VoxelData.waterHeight + (world.worldHeight * noise)) * GetFallOffMap(pos);

        noiseHeight = Mathf.Clamp(noiseHeight, 0, VoxelData.chunkHeight);

        return Mathf.FloorToInt(noiseHeight);
    }

    public static float GetWorldBaseHeight(World world, Vector2 pos, float scale, int octaves, float persistence, float lacunarity)
    {
        //  Very similar to the util 2d perlin noise, 
        //  Difference is we change the offset, to try and combat the lack of detail when dealing with large worlds

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
        //  Gets a value thorugh layered perlin noise
        //  Then takes that value to the power of itself, resulting in a sharp acending value, ie, steep mountains
        //  Then multiples this by another perlin noise, for more randomness

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

    public static bool Get3DPerlinCave(World world, int terrainHieght, Vector3 position, int yPos)
    {
        //  The cave system consists of 4 different layers
        //  The passage from surface (or top) to middle
        //  The middle layer
        //  The passage from middle to bottom
        //  The bottom

        if (Get3DPerlinBottom(world, position, yPos, 0.65f))
        {
            return true;
        }

        else if (yPos < 45f) return false;

        else if (Get3DPerlinMiddleToBottomPassages(world, position, yPos, 0.2f))
        {
            return true;
        }

        else if (yPos < 110f) return false;

        else if (Get3DPerlinMiddle(world, position, yPos, 0.75f))
        {
            return true;
        }

        else if (yPos < 130f) return false;

        else if (Get3DPerlinSurfaceToMiddlePassages(world, position, yPos, terrainHieght, 0.4f))
        {
            return true;
        }

        return false;
    }

    public static bool Get3DPerlinSurfaceToMiddlePassages(World world, Vector3 position, float yPos, int terrainHeight, float _treshold)
    {
        //  The surface to middle passage, should be noodle like caves connecting the surface to lower cave systems

        //  Take the y position and adds perlin to it, as to make the caves go up and down, changes are drastic as to make caves steep
        //  Then grabs the 3d perlin noise, using the changed y position, giving shape to cave walls

        //  Then gets a threshold value, this is 2d perlin, which is the main noise in this case, the higher ends of the perlin maps are made into caves
        //  Checks if cave is within the the right y positions, so it doesnt go too high or too low

        yPos += 50f * (1 - (Get2DPerlin(world, new Vector2(position.x, position.z), 1000f, 75f, 1, 0.5f, 1.7f, 10f) * 2f));

        float noise = Get3DPerlin(world, new Vector3(position.x, yPos, position.z), 2000f, 85f, 1, 0.4f, 3f);
        float treshold = -0.2f + Get2DPerlin(world, new Vector2(position.x, position.z), 200f, 50f, 1, 0.5f, 1.7f, 10f);

        treshold = Mathf.Clamp01(treshold);

        if (yPos <= 200f)
        {
            treshold += 1f - ((yPos - 200f) / 20f);
        }

        else if (yPos >= terrainHeight - 10f)
        {
            treshold += (yPos - (terrainHeight - 10f)) / 20f;
        }

        if (treshold + _treshold < noise) return true;

        return false;
    }

    public static bool Get3DPerlinMiddle(World world, Vector3 position, float yPos, float _treshold)
    {
        //  Takes the y positon and adds some perlin to it, as to make caves go up and down a bit
        //  Gets the main 3d noise, where caves are air
        //  Creates a threshold with increases with hieght, so cave walls lean inwards
        //  Checks if cave is within the right y position

        yPos += 20f * (1 - (Get2DPerlin(world, new Vector2(position.x, position.z), 500f, 150f, 2, 0.5f, 1.7f, 10f) * 2f));

        if (yPos > 220f) return false;

        float noise = Get3DPerlin(world, new Vector3(position.x, yPos, position.z), 1000f, 100f, 3, 0.4f, 3f);
        float treshold = (yPos - 150f) / VoxelData.chunkHeight;

        if (yPos <= 150f)
        {
            treshold += 1f - ((yPos - 150f) / 20f);
        }

        else if (yPos >= 200f)
        {
            treshold += (yPos - 200f) / 20f;
        }

        if (treshold + _treshold < noise) return true;

        return false;

    }

    public static bool Get3DPerlinMiddleToBottomPassages(World world, Vector3 position, float yPos, float _treshold)
    {
        //  Same as surface to middle passage, with differnt values, but same theory  

        yPos += 50f * (1 - (Get2DPerlin(world, new Vector2(position.x, position.z), 500f, 85f, 1, 0.5f, 1.7f, 10f) * 2f));

        if (yPos > 205f) return false;

        float noise = Get3DPerlin(world, new Vector3(position.x, yPos, position.z), 1000f, 100f, 1, 0.4f, 3f);
        float treshold =  -0.2f + Get2DPerlin(world, new Vector2(position.x, position.z), 250f, 50f, 1, 0.5f, 1.7f, 10f);

        treshold = Mathf.Clamp01(treshold);

        if (yPos <= 115f)
        {
            treshold += 1f - ((yPos - 115f) / 20f);
        }

        else if (yPos >= 135f)
        {
            treshold += (yPos - 135f) / 20f;
        }

        if (treshold + _treshold < noise) return true;

        return false;
    }

    public static bool Get3DPerlinBottom(World world, Vector3 position, float yPos, float _treshold)
    {
        //  Same as middle, with differnt values, but same theory  

        yPos += 20f * (1 - (Get2DPerlin(world, new Vector2(position.x, position.z), 500f, 150f, 2, 0.6f, 2.2f, 10f) * 2f));

        if (yPos > 120f) return false;

        float noise = Get3DPerlin(world, position, 2000f, 200f, 3, 0.5f, 2f);
        float treshold = (yPos) / VoxelData.chunkHeight;

        if (yPos <= 20f)
        {
            treshold += 1f - (yPos / 20f);
        }

        else if (yPos >= 100f)
        {
            treshold += (yPos - 100f) / 20f;
        }

        if (treshold + _treshold < noise) return true;

        return false;

    }

    #endregion

    #region humidity

    public static float Get2DPerlinHumidity(World world, Vector2 pos, float _offset, float scale, int octaves, float persistence, float lacunarity, float noiseCutoff)
    {
        //  Just perlin noise

        //  Needs work

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

        //  Needs work

        float longitude = pos.y;

        float distance = Mathf.Abs((VoxelData.WorldSizeInVoxel / 2) - longitude);

        float temperature = distance / (VoxelData.WorldSizeInVoxel / 2);

        temperature = Mathf.Clamp01(temperature);   

        return 1 - temperature;
    }

    #endregion

    #region Structure

    public static float Get2DPerlinStructureThreshold(Vector2 pos, float offset)
    {
        //  First grabs some perlin to act as the noise
        //  Then does some mathematical maths on with that perlin,
        //  So that small changes in position, equal large changes in numbers
        //  Then gets the modulo of the value, divided by the modulo as to always return a value between 0 and 1

        float perlin = Mathf.PerlinNoise((pos.x * 1111f) + offset + 0.1f, (pos.y * 1111f) + offset + 0.1f);

        float value = (pos.x * 1111f) + (pos.y * 1111f) + perlin;
        value *= perlin;
        value *= value;

        value %= 1000000f;

        value /= 1000000f;

        return value;
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
        //  Scary mathemathics

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

            float perlinValue = (AB + BC + AC + CA + BA + CB) / 6f;

            noise += perlinValue * amplituide;
            amplituide *= persistence;
            frequency *= lacunarity;
        }

        return noise;
    }

    #endregion
}