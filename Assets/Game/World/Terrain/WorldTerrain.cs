using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WorldTerrain
{
    public static byte GetVoxelBlockType(World world, Vector3 pos)
    {
        //  Goes through different blocks of code to determine terrainheight and blocktypes

        //  Constant, these are always true

        if (!IsVoxelInWorld(pos))
        {
            //  If outside world, return air
            return 0;
        }

        //  Biome, gets the biome this position is in and gets the adjacent biomes

        ChunkCoordinate cc = world.GetChunkCoordinateFromVector3(pos);
        Biome biome = world.biomeMap[cc.x, cc.z];
        Biome[] biomes = GetAdjacentBiomes(world, cc);

        #region math

        float height = 0;
        float startHeight = 0;
        float noiseScale = 0;
        float octaves = 0;
        float persistence = 0;
        float lacunarity = 0;

        #endregion

        int xPos = GetPositionInChunk(cc, pos).Item1;
        int zPos = GetPositionInChunk(cc, pos).Item2;

        if(biomes.Distinct().ToArray().Length > 1)
        {
            int division = 0;

            for (int i = 0; i < 9; i++)
            {
                if (biomes[i] != biome)
                {
                    float weight = GetBiomeWeight(Tuple.Create(xPos, zPos), Tuple.Create(BiomePositions[i, 0], BiomePositions[i, 1]));

                    #region math

                    height += Mathf.Lerp(biome.height, biomes[i].height, weight);
                    startHeight += Mathf.Lerp(biome.startHeight, biomes[i].startHeight, weight);
                    noiseScale += Mathf.Lerp(biome.noiseScale, biomes[i].noiseScale, weight);
                    octaves += Mathf.Lerp(biome.octaves, biomes[i].octaves, weight);
                    persistence += Mathf.Lerp(biome.persistence, biomes[i].persistence, weight);
                    lacunarity += Mathf.Lerp(biome.lacunarity, biomes[i].lacunarity, weight);

                    #endregion

                    division++;
                }
            }

            #region math

            height /= division;
            startHeight /= division;
            noiseScale /= division;
            octaves /= division;
            persistence /= division;
            lacunarity /= division;

            #endregion
        }

        else
        {
            height = biome.height;
            startHeight = biome.startHeight;
            noiseScale = biome.noiseScale;
            octaves = biome.octaves;
            persistence = biome.persistence;
            lacunarity = biome.lacunarity;
        }

        int terrainheight;
        int yPos = Mathf.FloorToInt(pos.y);

        //  Terrain, gets the terrainheigt through layered perlin noise

        terrainheight = Mathf.FloorToInt(Mathf.FloorToInt(height) * Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, noiseScale, Mathf.FloorToInt(octaves), persistence, lacunarity)) + Mathf.FloorToInt(startHeight);

        //  Falloff, terrain thats "falloff" worldsize from spawn, starts to get multiplied by a value aproaching 0,
        //  meaning the terrain falls off to be surrounded by water

        if(Noise.GetFallOffMap(new Vector2(pos.x, pos.z)) < world.falloff)
        {
            terrainheight = Mathf.FloorToInt((1 / world.falloff) * terrainheight * Noise.GetFallOffMap(new Vector2(pos.x, pos.z)));
        }

        //  Block, determines the blocktype

        if (yPos == terrainheight) return biome.surfaceBlockId;
        else if (yPos < terrainheight && yPos > terrainheight - 4) return biome.subSurfaceBlockId;
        else if (yPos < terrainheight) return 3;
        else if (yPos < 64) return 9;
        else return 0;
    }

    public static Biome GetBiome(World world, Vector3 pos)
    {
        //  Value given by perlin noise runs through thresholds

        Climate climate = GetClimate(world, pos);

        float noise = Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, climate.noiseScale, climate.octaves, climate.persistence, climate.lacunarity);

        for (int i = 0; i < climate.biomes.Length; i++)
        {
            if (noise >= climate.thresholds[i]) return climate.biomes[i];
        }

        return climate.biomes.Last();
    }

    private static Climate GetClimate(World world, Vector3 pos)
    {
        //  Value depending on z pos in world

        for (int i = 0; i < world.climates.Length; i++)
        {
            if (pos.z / VoxelData.WorldSizeInVoxel >= world.thresholds[i]) return world.climates[i];
        }

        return world.climates.Last();
    }

    //  Blend

    private static float GetBiomeWeight(Tuple<int, int> VoxelPos, Tuple<int, int> biomePos)
    {
        if (biomePos.Item1 != 0 && biomePos.Item2 != 0)
        {
            float xWeight = (float)(VoxelData.chunkWidth - Mathf.Abs(biomePos.Item1 - VoxelPos.Item1)) / VoxelData.chunkWidth;
            float zWeight = (float)(VoxelData.chunkWidth - Mathf.Abs(biomePos.Item2 - VoxelPos.Item2)) / VoxelData.chunkWidth;

            /*
            Debug.Log("xWeight " + xWeight);
            Debug.Log((VoxelData.chunkWidth - Mathf.Abs(biomePos.Item1 - VoxelPos.Item1)));
            Debug.Log((Mathf.Abs(biomePos.Item1 - VoxelPos.Item1)));
            Debug.Log((biomePos.Item1));
            Debug.Log((VoxelPos.Item1));

            Debug.Log("zWeight " + zWeight);
            Debug.Log((VoxelData.chunkWidth - Mathf.Abs(biomePos.Item2 - VoxelPos.Item2)));

            Debug.Log("xz");
            */

            return (xWeight + zWeight) / 2;
        }

        else if (biomePos.Item1 != 0)
        {
            float xWeight = (float)(VoxelData.chunkWidth - Mathf.Abs(biomePos.Item1 - VoxelPos.Item1)) / VoxelData.chunkWidth;
            return xWeight;
        }

        else if (biomePos.Item2 != 0)
        {
            float zWeight = (float)(VoxelData.chunkWidth - Mathf.Abs(biomePos.Item2 - VoxelPos.Item2)) / VoxelData.chunkWidth;
            return zWeight;
        }

        else
        {
            return 1f;
        }     
    }

    private static Tuple<int, int> GetPositionInChunk(ChunkCoordinate cc, Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x) - (cc.x * VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z) - (cc.z * VoxelData.chunkWidth);

        return Tuple.Create(x, z);
    }

    private static Biome[] GetAdjacentBiomes(World world, ChunkCoordinate cc)
    {
        Biome[] biomes = new Biome[9]
        {
            world.biomeMap[cc.x + 1, cc.z + 1], //  top right
            world.biomeMap[cc.x, cc.z + 1], //  top
            world.biomeMap[cc.x - 1, cc.z + 1], //  top left
            world.biomeMap[cc.x + 1, cc.z], //  right
            world.biomeMap[cc.x, cc.z], // middle
            world.biomeMap[cc.x - 1, cc.z], // left
            world.biomeMap[cc.x + 1, cc.z - 1], //  bottom right
            world.biomeMap[cc.x, cc.z - 1], //  bottom
            world.biomeMap[cc.x - 1, cc.z - 1] //   bottom left
        };

        return biomes;
    }

    private static readonly int[,] BiomePositions = new int[9, 2]
    {
        {VoxelData.chunkWidth, VoxelData.chunkWidth},
        {0, VoxelData.chunkWidth},
        {1, VoxelData.chunkWidth},
        {VoxelData.chunkWidth, 0},
        {0, 0},
        {1, 0},
        {VoxelData.chunkWidth, 1},
        {0, 1},
        {1, 1}
    };

    //

    private static bool IsVoxelInWorld(Vector3 pos)
    {
        //  Checks if voxel is inside world

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxel && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxel)
            return true;
        else
            return false;
    }
}
