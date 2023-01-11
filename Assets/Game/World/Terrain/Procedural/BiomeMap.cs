using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BiomeMap
{
    //  Divides the world into biomes with voroni noise,
    //  Biomes are placed at points, within their climates, 
    //  Edges of map is water, so world is surrounded by ocean
    //  Creates a biome map where each chunk is assigned a biome

    public static Biome[,] GetBiomeMap(World world)
    {
        //  Creates a biome map with size of worldsize and also creates a layer around this, so edges of map always have surrounding biomes

        Climate[,] climateMap = new Climate[VoxelData.worldSizeInChunks + 2, VoxelData.worldSizeInChunks + 2];
        Biome[,] biomeMap = new Biome[VoxelData.worldSizeInChunks + 2, VoxelData.worldSizeInChunks + 2];

        #region Climatemap

        //  Each chunk will be assigen a climate, depedning on temperature and humidity

        for (int x = 0; x < VoxelData.worldSizeInChunks + 2; x++) //    Assigning each chunk a climate
        {
            for (int z = 0; z < VoxelData.worldSizeInChunks + 2; z++)
            {

                climateMap[x, z] = GetClimate(world.climates, x, z);
            }
        }

        #endregion

        #region Biomemap

        //  Creates a list of biomeOrigins, which contains a vector3 and a biome,
        //  Position of origins are chose at random, within their climates

        List<Tuple<Vector3, Biome>> biomes = GetBiomePositions(world, climateMap, world.climates);

        //  Each chunk will check which origin is closest, can be adjusted by weight (so we can adjust size of biomes)
        //  So biomes will be created around these origins

        for (int x = 0; x < VoxelData.worldSizeInChunks + 2; x++) //    Assigning each chunk a biome
        {
            for (int z = 0; z < VoxelData.worldSizeInChunks + 2; z++)
            {
                biomeMap[x, z] = GetBiome(world, biomes, x, z);
            }
        }

        #endregion

        return biomeMap;
    }

    private static float GetTemperature(Vector2 pos)
    {
        //  Gets the distance from equator
        //  Then assign temperature depending on distance

        float distanceToEquator = Vector2.Distance(pos, new Vector3(VoxelData.WorldSizeInVoxel / 2f, pos.y)) + 1f;
        float temperature = 100f - (distanceToEquator / (VoxelData.landSizeInChunks * VoxelData.chunkWidth / 200f));

        return temperature;
    }

    private static float GetHumidity(Vector2 pos)
    {
        //  Not implemented

        return 0;
    }

    private static Climate GetClimate(Climate[] climates, int x, int z)
    {
        //  Gets the temperature and humidity in chunk
        //  Loops through each climate and checks which is a best match for the chunk conditions

        Vector2 chunkPos = new Vector3(x * VoxelData.chunkWidth, z * VoxelData.chunkWidth);

        float temperature = GetTemperature(chunkPos);
        float humidity = GetHumidity(chunkPos);

        Climate currentClimate = null;
        float currentTemperatureDistance = Mathf.Infinity;

        foreach (Climate climate in climates)
        {
            if(Mathf.Abs(temperature - climate.temperature) < currentTemperatureDistance)
            {
                currentClimate = climate;
                currentTemperatureDistance = Mathf.Abs(temperature - climate.temperature);
            }
        }

        return currentClimate;
    }

    private static List<Tuple<Vector3, Biome>> GetBiomePositions(World world, Climate[,] climateMap, Climate[] climates)
    {
        List<Tuple<Vector3, Biome>> biomes = new List<Tuple<Vector3, Biome>>();

        #region land

        //  For each climate
        //  For each biome in climate
        //  For each amount of biome
        //  Place a biome origin if biome origin is inside right climate

        int landMinRange = VoxelData.mapEdgeSizeInChunks + VoxelData.oceanSizeInChunks;
        int landMaxRange = landMinRange + VoxelData.landSizeInChunks;
        int chunkWidth = VoxelData.chunkWidth;

        for (int c = 0; c < climates.Length; c++)
        {
            List<Vector3> positions = new List<Vector3>();

            int amountOfPositions = 0;
            List<Biome> amountOfBiomes = new List<Biome>();

            for (int b = 0; b < climates[c].biomes.Length; b++)
            {
                for (int a = 0; a < climates[c].biomes[b].amount; a++)
                {
                    bool foundPos = false;

                    int tries = 0;

                    while (!foundPos)
                    {
                        int x = UnityEngine.Random.Range(landMinRange, landMaxRange);
                        int z = UnityEngine.Random.Range(landMinRange, landMaxRange);

                        if (climateMap[x, z] == climates[c])
                        {
                            positions.Add(new Vector3(x * chunkWidth, 0, z * chunkWidth));

                            amountOfPositions++;
                            amountOfBiomes.Add(climates[c].biomes[b]);

                            foundPos = true;
                        }

                        tries++;

                        if(tries > 1000)
                        {
                            Debug.Log("Cant find spot for " + climates[c].biomes[b].name + "  boss");
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

        #endregion

        #region ocean

        int oceanMinRange = VoxelData.mapEdgeSizeInChunks;
        int oceanMaxRange = landMinRange + VoxelData.oceanSizeInChunks + VoxelData.landSizeInChunks + VoxelData.oceanSizeInChunks;

        int amount = world.oceanAmountPerSide;



        for(int i = 0; i < amount; i++)
        {
            int x = UnityEngine.Random.Range(oceanMinRange, oceanMaxRange);
            int z = UnityEngine.Random.Range(oceanMinRange, oceanMaxRange);

            Vector3 position = new Vector3(x * chunkWidth, 0, z * chunkWidth);

            biomes.Add(Tuple.Create(position, world.oceanReference));
        }

        #endregion

        return biomes;
    }

    private static Biome GetBiome(World world, List<Tuple<Vector3, Biome>> biomes, int x, int z)
    {
        //  First checks if chunk is on edge of map, if so go ocean
        //  Starts checking each biome origin for which is closest
        //  Stores the closest distance, and compares with new distance
        //  Distances are multiplied by a weight so certain biomes have less reach (are smaller)

        int worldSize = VoxelData.worldSizeInChunks;
        int edgeSize = VoxelData.mapEdgeSizeInChunks;

        if (x <= edgeSize || x >= worldSize - edgeSize || z <= edgeSize || z >= worldSize - edgeSize)
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
