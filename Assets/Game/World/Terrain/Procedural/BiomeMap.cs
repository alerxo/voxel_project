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
        //  Also creates a map for rivers, as these are generated seperatly

        SubBiome[,] subBiomeMap = new SubBiome[VoxelData.worldSizeInChunks + 2, VoxelData.worldSizeInChunks + 2];
        bool[,] riverMap = GetRiverMap(world);

        //  Each chunk will be assigen a biome, depedning on temperature, humidity and height
        //  If its a river, disregard above

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

        int height = GetHeight(world, chunkPos);
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
            Debug.Log("At: " + x + ", " + z);

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
        //  Tmperature by perlin noise multiplied

        return Noise.GetTemperature(pos);
    }

    private static float GetHumidity(World world, Vector2 pos)
    {
        //  Humidity is random noise, multiplied by falloff, so closer to edge of world, which should be ocean is more humid

        return Noise.Get2DPerlinHumidity(world, pos, 1000, VoxelData.worldSizeInChunks, 1, 1, 1, 1);
    }

    private static int GetHeight(World world, Vector2 pos)
    {
        //  Terrainheight by perlin noise multiplied by falloff

        return Noise.Get2DPerlinWorld(world, pos);
    }

    private static bool[,] GetRiverMap(World world)
    {
        //  Creates a map of rivers using source chunks and creating worm from that chunk

        bool[,] riverMap = new bool[VoxelData.worldSizeInChunks + 2, VoxelData.worldSizeInChunks + 2];

        //  Creating the source chunks
        //  Go trough each chunk, if the height and humidity are high enough, add it to the list

        List<Tuple<int, int>> sourceChunks = new List<Tuple<int, int>>();

        for (int x = 0; x < VoxelData.worldSizeInChunks + 2; x++)
        {
            for (int z = 0; z < VoxelData.worldSizeInChunks + 2; z++)
            {
                Vector2 pos = new((x * VoxelData.chunkWidth) + VoxelData.chunkWidth / 2, (z * VoxelData.chunkWidth) + VoxelData.chunkWidth / 2);

                if (GetHeight(world, pos) > world.riverSourceHeightLimit && GetHumidity(world, pos) > 0.8f)
                {
                    sourceChunks.Add(Tuple.Create(x, z));
                }
            }
        }

        //  Goes thorugh the source block list, grabs first in list, then creates a worm
        //  Adds a random number to "s" as to not grab source chunks that are next to each other,
        //  as in source chunk list, entire mountains can be added, and we only want one chunk

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
        //  Starts from a souce chunk and gets adjacent chunk with lowest height, as to simulate rver flowing downwards,
        //  If current chunk is the highest out of adjacent chunks, end worm

        List<Tuple<int, int>> riverWorm = new List<Tuple<int, int>>();

        int currentX = x;
        int currentZ = z;

        bool riverEndFound = false;

        //  Fail safe so we dont accidentally create infinite loop

        int failSafe = 50;
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
        //  Gets the lowest adjacent chunk

        int x = currentX;
        int z = currentZ;

        Tuple<int, int> lowestAdjacent = GetLowestAdjacent(world, x, z);

        x = lowestAdjacent.Item1;
        z = lowestAdjacent.Item2;

        return Tuple.Create(x, z);
    }

    private static Tuple<int, int> GetLowestAdjacent(World world, int x, int z)
    {
        //  Creates a list of adjacent chunk and their heights, loops through and gets lowest

        List<ChunkCoordinate> adjacentPos = new List<ChunkCoordinate>
        {
            new ChunkCoordinate(x, z),
            new ChunkCoordinate(x, z + 1),
            new ChunkCoordinate(x + 1, z),
            new ChunkCoordinate(x - 1, z),
            new ChunkCoordinate(x, z - 1)

        };

        List<int> adjacentsHeight = new List<int>
        {
            WorldTerrain.GetRiverHeight(world, adjacentPos[0]),
            WorldTerrain.GetRiverHeight(world, adjacentPos[1]),
            WorldTerrain.GetRiverHeight(world, adjacentPos[2]),
            WorldTerrain.GetRiverHeight(world, adjacentPos[3]),
            WorldTerrain.GetRiverHeight(world, adjacentPos[4])
        };

        int currentHeight = VoxelData.chunkHeight;
        int currentAdjacent = 0;

        for (int i = 0; i < 5; i++)
        {
            if (adjacentsHeight[i] < currentHeight)
            {
                currentHeight = adjacentsHeight[i];
                currentAdjacent = i;
            }
        }

        return Tuple.Create(adjacentPos[currentAdjacent].x, adjacentPos[currentAdjacent].z);
    }
}