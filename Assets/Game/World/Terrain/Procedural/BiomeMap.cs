using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BiomeMap
{
    //  Divides the world into biomes using temperature, humidity and terrainheight parameters
    //  Adds these to a two-dimenstional array, for x and z coordinates, which acts as a map

    public static SubBiome[,] GetBiomeMap(World world)
    {
        //  Creates a biome map with size of worldsize and also creates a layer around this, so edges of map always have surrounding biomes

        SubBiome[,] subBiomeMap = new SubBiome[VoxelData.worldSizeInChunks + 2, VoxelData.worldSizeInChunks + 2];
        bool[,] riverMap = GetRiverMap(world);

        //  Each chunk will be assigen a climate, depedning on temperature, humidity and height

        for (int x = 0; x < VoxelData.worldSizeInChunks + 2; x++)
        {
            for (int z = 0; z < VoxelData.worldSizeInChunks + 2; z++)
            {
                if (riverMap[x, z])
                {
                    subBiomeMap[x, z] = world.riverReference;
                }

                else
                {
                    subBiomeMap[x, z] = GetBiome(world, world.biomes, x, z);
                }
            }
        }

        return subBiomeMap;
    }

    private static SubBiome GetBiome(World world, Biome[] biomes, int x, int z)
    {
        //  Gets the temperature, humidity and height in chunk
        //  Goes through each parameter and removes biomes that don't fit
        //  Only one should be left, and we get a subbiome of that

        Vector2 chunkPos = new Vector2(x * VoxelData.chunkWidth, z * VoxelData.chunkWidth);

        float height = GetHeight(world, chunkPos);
        float humidity = GetHumidity(world, chunkPos);
        float temperature = GetTemperature(chunkPos);

        List<Biome> tempBiomes = biomes.ToList();
        List<Biome> biomesPostTemperature = new List<Biome>();
        List<Biome> biomesPostHumidity = new List<Biome>();
        List<Biome> biomesPostHeight = new List<Biome>();

        for (int i = 0; i < tempBiomes.Count; i++)
        {
            if (temperature >= tempBiomes[i].minTemperature && tempBiomes[i].maxTemperature >= temperature)
            {
                biomesPostTemperature.Add(tempBiomes[i]);
            }
        }

        for (int i = 0; i < biomesPostTemperature.Count; i++)
        {
            if (humidity >= biomesPostTemperature[i].minHumidity && biomesPostTemperature[i].maxHumidity >= humidity)
            {
                biomesPostHumidity.Add(biomesPostTemperature[i]);
            }
        }

        for (int i = 0; i < biomesPostHumidity.Count; i++)
        {
            if (height >= biomesPostHumidity[i].minHeight && biomesPostHumidity[i].maxHeight >= height)
            {
                biomesPostHeight.Add(biomesPostHumidity[i]);
            }
        }

        if (biomesPostHeight.Count > 1)
        {
            Debug.Log("Error in BiomeMap/GetBiome: Too many biomes fit parameter");
            Debug.Log(temperature + " " + humidity + " " + height);

            for (int i = 0; i < biomesPostHeight.Count; i++)
            {
                Debug.Log(biomesPostHeight[i].biomeName);
            }
        }

        else if (biomesPostHeight.Count == 0)
        {
            Debug.Log("Error in BiomeMap/GetBiome: No biomes fit parameters");
            Debug.Log(temperature + " " + humidity + " " + height);

            return biomes[4].subBiomes[0];
        }

        return GetSubBiome(world, biomesPostHeight[0], x, z);
    }

    private static SubBiome GetSubBiome(World world, Biome biome, int x, int z)
    {
        //  Gets noise at chunk pos, loops through each subbiome and checks which fits
        //  Subbiomes are arranged in ascending order of thresholds
        //  First subbiome whos treshold is bigger than noise, is the one that fits, and is returned

        float noise = Noise.Get2DPerlin(world, new Vector2(x, z), 2000, 50, 1, 1, 1, 1);

        foreach (SubBiome subBiome in biome.subBiomes)
        {
            if (subBiome.threshold >= noise)
            {
                return subBiome;
            }
        }

        Debug.Log("Error in BiomeMap/GetSubBiome: No SubBiome fit threshold");
        Debug.Log(noise);

        return biome.subBiomes[0];
    }

    private static float GetTemperature(Vector2 pos)
    {
        //  Gets the distance from equator
        //  Then assign temperature depending on distance

        float longitude = pos.y;

        float distance = Mathf.Abs((VoxelData.WorldSizeInVoxel / 2) - longitude);

        float temperature = distance / (VoxelData.WorldSizeInVoxel / 2);

        if (temperature < 0)
        {
            //Debug.Log("Temperature is too low: " + temperature);
            temperature = 0;
        }

        else if (temperature > 1)
        {
            //Debug.Log("Temperature is too big: " + temperature);
            temperature = 1;
        }

        return 1 - temperature;
    }

    private static float GetHumidity(World world, Vector2 pos)
    {
        //  Humidity is random noise, multiplied by falloff, so closer to edge of world, which should be ocean is more humid

        float humidity = Noise.Get2DPerlin(world, pos, 1000, VoxelData.worldSizeInChunks, 1, 1, 1, 1) * (1 - Noise.GetFallOffMap(pos));

        if (humidity < 0)
        {
            //Debug.Log("Humidity is too low: " + humidity);
            humidity = 0;
        }

        else if (humidity > 1)
        {
            //ebug.Log("Humidity is too big: " + humidity);
            humidity = 1;
        }

        return humidity;
    }

    private static float GetHeight(World world, Vector2 pos)
    {
        //  Terrainheight by perlin noise multiplied by falloff

        float height = Noise.Get2DPerlinHighResolution(world, world.resolution, pos, 0, world.noiseScale, world.octaves, world.persistence, world.lacunarity, world.noiseCutoff) * Noise.GetFallOffMap(pos);

        if (height < 0)
        {
            //Debug.Log("Height is too low: " + height);
            height = 0;
        }

        else if (height > 1)
        {
            //Debug.Log("Height is too big: " + height);
            height = 1;
        }

        return height;
    }


    private static bool[,] GetRiverMap(World world)
    {
        bool[,] riverMap = new bool[VoxelData.worldSizeInChunks + 2, VoxelData.worldSizeInChunks + 2];

        List<Tuple<int, int>> sourceChunks = new List<Tuple<int, int>>();

        for (int x = 0; x < VoxelData.worldSizeInChunks + 2; x++)
        {
            for (int z = 0; z < VoxelData.worldSizeInChunks + 2; z++)
            {
                if (GetHeight(world, new Vector2(x * VoxelData.chunkWidth, z * VoxelData.chunkWidth)) > world.riverReference.threshold)
                {
                    sourceChunks.Add(Tuple.Create(x, z));
                }
            }
        }

        for (int s = 0; s < sourceChunks.Count; s++)
        {
            List<Tuple<int, int>> riverWorm = GetRiverWorm(world, sourceChunks[s].Item1, sourceChunks[s].Item2);

            for (int w = 0; w < riverWorm.Count; w++)
            {
                if (Voxel_Util.IsChunkInWorld(new ChunkCoordinate(riverWorm[w].Item1, riverWorm[w].Item2)))
                {
                    riverMap[riverWorm[w].Item1, riverWorm[w].Item2] = true;
                }
            }

            s += UnityEngine.Random.Range(0, (int)world.riverReference.noiseScale);
        }

        return riverMap;
    }

    private static List<Tuple<int, int>> GetRiverWorm(World world, int x, int z)
    {
        List<Tuple<int, int>> riverWorm = new List<Tuple<int, int>>();

        int currentX = x;
        int currentZ = z;

        bool riverEndFound = false;

        int failSafe = 10;
        int riverLength = 0;

        while (!riverEndFound)
        {
            riverWorm.Add(Tuple.Create(currentX, currentZ));

            Tuple<int, int> nextRiverChunk = GetNextRiverChunk(world, currentX, currentZ);

            if ((nextRiverChunk.Item1 == currentX && nextRiverChunk.Item2 == currentZ) || riverLength > failSafe)
            {
                riverEndFound = true;
            }

            currentX = nextRiverChunk.Item1;
            currentZ = nextRiverChunk.Item2;

            riverLength++;
        }

        return riverWorm;
    }

    private static Tuple<int, int> GetNextRiverChunk(World world, int currentX, int currentZ)
    {
        int x = currentX;
        int z = currentZ;

        Tuple<int, int> lowestAdjacent = GetLowestAdjacent(world, x, z);

        x = lowestAdjacent.Item1;
        z = lowestAdjacent.Item2;

        return Tuple.Create(x, z);
    }

    private static Tuple<int, int> GetLowestAdjacent(World world, int x, int z)
    {
        int cw = VoxelData.chunkWidth;
        int hcw = VoxelData.chunkWidth / 2;

        List<Vector2> adjacentPos = new List<Vector2>
        {
            new Vector2((x * cw) + hcw, ((z + 1) * cw) + hcw),
            new Vector2(((x + 1) * cw) + hcw, (z * cw) + hcw),
            new Vector2((x * cw) + hcw, ((z - 1) * cw) + hcw),
            new Vector2(((x - 1) * cw) + hcw, (z * cw) + hcw),
            new Vector2((x * cw) + hcw, (z * cw) + hcw)
        };

        List<float> adjacentsHeight = new List<float>
        {
            GetHeight(world, adjacentPos[0]),
            GetHeight(world, adjacentPos[1]),
            GetHeight(world, adjacentPos[2]),
            GetHeight(world, adjacentPos[3]),
            GetHeight(world, adjacentPos[4])
        };

        float currentHeight = 1f;
        int currentAdjacent = 0;

        for (int i = 0; i < 5; i++)
        {
            if (adjacentsHeight[i] < currentHeight)
            {
                currentHeight = adjacentsHeight[i];
                currentAdjacent = i;
            }
        }

        return Tuple.Create((int)adjacentPos[currentAdjacent].x / cw, (int)adjacentPos[currentAdjacent].y / cw);
    }
}