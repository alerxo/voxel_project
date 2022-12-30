using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BiomeMap
{
    //  Divides the world into biomes accoring to all climates and biomes in those climate, and also amount of said biomes
    //  Edges of map is water, so world is surrounded by ocean
    //  Creates a biome map where each chunk is assigned a biome

    public static Biome[,] GetBiomeMap(World world)
    {
        //  Creates a biome map with size of worldsize and also creates a layer around this
        //  Creates a list of biomeOrigins, which contains a vector 3 and a biome,
        //  Position of origins are chose at random, with some parameters
        //  Each chunk will check which origin is closest, can be adjusted by weight (so we can adjust size of biomes)
        //  So biomes will be created around these origins

        Climate[,] climateMap = new Climate[VoxelData.WorldSizeInChunks + 2, VoxelData.WorldSizeInChunks + 2];
        Biome[,] biomeMap = new Biome[VoxelData.WorldSizeInChunks + 2, VoxelData.WorldSizeInChunks + 2];

        List<Tuple<Vector3, Climate>> climates = GetClimatePositions(world.climates);

        Debug.Log("Amount of climates: " + climates.Count);

        for (int x = 0; x < VoxelData.WorldSizeInChunks + 2; x++) //    Assigning each chunk a climate
        {
            for (int z = 0; z < VoxelData.WorldSizeInChunks + 2; z++)
            {
                climateMap[x, z] = GetClimate(world, climates, x, z);
            }
        }

        Debug.Log("Climates mapped");

        List<Tuple<Vector3, Biome>> biomes = GetBiomePositions(climateMap, world.climates);

        Debug.Log("Amount of biomes: " + biomes.Count);    

        for (int x = 0; x < VoxelData.WorldSizeInChunks + 2; x++) //    Assigning each chunk a biome
        {
            for (int z = 0; z < VoxelData.WorldSizeInChunks + 2; z++)
            {
                biomeMap[x, z] = GetBiome(world, biomes, x, z);
            }
        }

        Debug.Log("Biomes mapped");

        return biomeMap;
    }



    private static List<Tuple<Vector3, Climate>> GetClimatePositions(Climate[] _climates)
    {
        List<Tuple<Vector3, Climate>> climates = new List<Tuple<Vector3, Climate>>();

        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < _climates.Length; i++)
        {
            int x = UnityEngine.Random.Range(0, VoxelData.WorldSizeInVoxel);
            int z = UnityEngine.Random.Range(0, VoxelData.WorldSizeInVoxel);

            positions.Add(new Vector3(x, 0, z));
        }

        for (int i = 0; i < _climates.Length; i++)
        {
            climates.Add(Tuple.Create(positions[i], _climates[i]));
        }

        return climates;
    }

    private static Climate GetClimate(World world, List<Tuple<Vector3, Climate>> climates, int x, int z)
    {
        //  First checks if chunk is inside of the ocean
        //  Then starts checking each biome origin for which is closest
        //  Stores the closest distance, and compares with new distance
        //  Distances are multiplied by a weight so certain biomes have less reach (are smaller)

        Vector3 chunkPos = new Vector3(x * VoxelData.chunkWidth, 0, z * VoxelData.chunkWidth);

        Vector3 currentClimatePos = climates[0].Item1;
        Climate currentClimate = climates[0].Item2;

        foreach (Tuple<Vector3, Climate> climate in climates)
        {
            if (Vector3.Distance(chunkPos, climate.Item1) < Vector3.Distance(chunkPos, currentClimatePos))
            {
                currentClimatePos = climate.Item1;
                currentClimate = climate.Item2;
            }
        }

        return currentClimate;
    }



    private static List<Tuple<Vector3, Biome>> GetBiomePositions(Climate[,] climateMap, Climate[] climates)
    {
        List<Tuple<Vector3, Biome>> biomes = new List<Tuple<Vector3, Biome>>();

        for(int c = 0; c < climates.Length; c++)
        {
            List<Vector3> positions = new List<Vector3>();

            int amountOfPositions = 0;
            List<Biome> amountOfBiomes = new List<Biome>();

            for (int b = 0; b < climates[c].biomes.Length; b++)
            {
                for (int a = 0; a < climates[c].biomes[b].amount; a++)
                {
                    bool foundPos = false;

                    while (!foundPos)
                    {
                        int x = UnityEngine.Random.Range(0, VoxelData.WorldSizeInChunks + 2);
                        int z = UnityEngine.Random.Range(0, VoxelData.WorldSizeInChunks + 2);

                        if (climateMap[x, z] == climates[c])
                        {
                            positions.Add(new Vector3(x * VoxelData.chunkWidth, 0, z * VoxelData.chunkWidth));

                            amountOfPositions++;
                            amountOfBiomes.Add(climates[c].biomes[b]);

                            foundPos = true;
                        }
                    }
                }
            }

            for (int b = 0; b < amountOfPositions; b++)
            {
                biomes.Add(Tuple.Create(positions[b], amountOfBiomes[b]));
            }
        }

        return biomes;
    }

    private static Biome GetBiome(World world, List<Tuple<Vector3, Biome>> biomes, int x, int z)
    {
        //  First checks if chunk is inside of the ocean
        //  Then starts checking each biome origin for which is closest
        //  Stores the closest distance, and compares with new distance
        //  Distances are multiplied by a weight so certain biomes have less reach (are smaller)

        if (x < VoxelData.OceanSizeInChunks || x > VoxelData.WorldSizeInChunks - VoxelData.OceanSizeInChunks || z < VoxelData.OceanSizeInChunks || z > VoxelData.WorldSizeInChunks - VoxelData.OceanSizeInChunks)
            return world.oceanReference;

        Vector3 chunkPos = new Vector3(x * VoxelData.chunkWidth, 0, z * VoxelData.chunkWidth);

        Vector3 currentBiomePos = biomes[0].Item1;
        Biome currentBiome = biomes[0].Item2;

        foreach(Tuple<Vector3, Biome> biome in biomes)
        {
            if (Vector3.Distance(chunkPos, biome.Item1) < Vector3.Distance(chunkPos, currentBiomePos))
            {
                currentBiomePos = biome.Item1;
                currentBiome = biome.Item2;
            }
        }

        return currentBiome;
    }
}
