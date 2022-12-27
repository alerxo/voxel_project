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

        Biome[,] biomeMap = new Biome[VoxelData.WorldSizeInChunks + 2, VoxelData.WorldSizeInChunks + 2];

        List<Tuple<Vector3, Biome>> biomeOrigins = new List<Tuple<Vector3, Biome>>();

        foreach(Climate climate in world.climates) //  Creating the biome origins
        {
            foreach(Biome biome in climate.biomes)
            {
                for(int i = 0; i < biome.amount; i++)
                {
                    biomeOrigins.Add(Tuple.Create(GetBiomePos(biomeOrigins), biome));
                }
            }
        }

        for (int x = 0; x < VoxelData.WorldSizeInChunks + 2; x++) //    Assigning each chunk a biome
        {
            for (int z = 0; z < VoxelData.WorldSizeInChunks + 2; z++)
            {
                biomeMap[x, z] = GetBiome(world, biomeOrigins, x, z);
            }
        }

        return biomeMap;
    }

    private static Vector3 GetBiomePos(List<Tuple<Vector3, Biome>> biomeOrigins)
    {
        //  Will try to get a positon, which is outside of ocean
        //  Checks postion against each existing biomeOrigin, if its too close (inside min biome size) try again
        //  If it has tried too many times, just give up, just so we can break out of loop incase possible postion doesn't exist

        bool foundOrigin = false;

        int x = 0;
        int z = 0;

        int tries = 0;

        while (!foundOrigin)
        {
            x = UnityEngine.Random.Range(VoxelData.WorldSizeInVoxel + VoxelData.OceanSizeInVoxel, VoxelData.WorldSizeInChunks - VoxelData.OceanSizeInVoxel);
            z = UnityEngine.Random.Range(VoxelData.WorldSizeInVoxel + VoxelData.OceanSizeInVoxel, VoxelData.WorldSizeInChunks - VoxelData.OceanSizeInVoxel);

            foreach(Tuple<Vector3, Biome> origin in biomeOrigins)
            {
                if(Vector3.Distance(new Vector3(x, 0, z), origin.Item1) > origin.Item2.minBiomeSizeInChunks * VoxelData.chunkWidth)
                {
                    foundOrigin = true;
                }
            }

            tries++;

            if (tries > 100)
            {
                foundOrigin = true;
            }
        }

        return new Vector3(x, 0, z);
    }

    private static Biome GetBiome(World world, List<Tuple<Vector3, Biome>> biomeOrigins, int x, int z)
    {
        //  First checks if chunk is inside of the ocean
        //  Then starts checking each biome origin for which is closest
        //  Stores the closest distance, and compares with new distance
        //  Distances are multiplied by a weight so certain biomes have less reach (are smaller)

        if (x < VoxelData.OceanSizeInChunks || x > VoxelData.WorldSizeInChunks - VoxelData.OceanSizeInChunks || z < VoxelData.OceanSizeInChunks || z > VoxelData.WorldSizeInChunks - VoxelData.OceanSizeInChunks)
            return world.oceanReference;

        Vector3 biomePos = new Vector3(x * VoxelData.chunkWidth, 0, z * VoxelData.chunkWidth);

        Biome closestBiomeOrigin = biomeOrigins[0].Item2;
        Vector3 currentBiomePos = biomeOrigins[0].Item1;

        foreach(Tuple<Vector3, Biome> biomeOrigin in biomeOrigins)
        {
            if (Vector3.Distance(biomePos, biomeOrigin.Item1) * closestBiomeOrigin.weight < Vector3.Distance(biomePos, currentBiomePos) * biomeOrigin.Item2.weight)
            {
                currentBiomePos = biomeOrigin.Item1;
                closestBiomeOrigin = biomeOrigin.Item2;
            }
        }

        return closestBiomeOrigin;
    }

}
