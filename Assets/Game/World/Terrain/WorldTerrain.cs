using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WorldTerrain
{
    public static byte GetVoxelBlockType(World world, Vector3 pos)
    {
        //  Goes through different parameters to determine blocktype

        #region Constants
        //  If outside of world, block is air

        if (!IsVoxelInWorld(pos))
        {
            //  If outside world, return air
            return 0;
        }

        #endregion

        #region Biome

        //  Gets chunkBiome and adjacentBiomes
        //  Gets the positin inside the chunk, as to be able to blend each block in chunk
        //  Checks if biome should blend, then blends noise values accordinly, else set noise values to chunkBiome

        ChunkCoordinate cc = world.GetChunkCoordinateFromVector3(pos);
        Biome chunkBiome = world.biomeMap[cc.x + 1, cc.z + 1];
        Biome[] adjacentBiomes = GetAdjacentBiomes(world, cc);

        #region math

        //  Creates the biome noise values

        float height = 0;
        float startHeight = 0;
        float noiseScale = 0;
        float octaves = 0;
        float persistence = 0;
        float lacunarity = 0;
        float noiseCutoff = 0;

        #endregion

        int xPos = GetPositionInChunk(cc, pos).Item1;
        int zPos = GetPositionInChunk(cc, pos).Item2;

        if(IsBlend(adjacentBiomes, chunkBiome))
        {
            int division = 0;
            int duplicates = 0;
            List<Biome> repeatingBiomes = new List<Biome>();

            for(int i = 0; i < 9; i++)
            {
                if (repeatingBiomes.Contains(adjacentBiomes[i]) && adjacentBiomes[i] != chunkBiome)
                {
                    duplicates++;
                } 
                else if (adjacentBiomes[i] != chunkBiome)
                {
                    repeatingBiomes.Add(adjacentBiomes[i]);
                    duplicates++;
                }   
            }

            for (int i = 0; i < 9; i++)
            {
                if (adjacentBiomes[i] != chunkBiome)
                {
                    float weight = GetBiomeWeight(Tuple.Create(xPos, zPos), Tuple.Create(BiomePositions[i, 0], BiomePositions[i, 1])) / duplicates;

                    if (weight < 0.2f && duplicates > 1) weight = 0;

                    #region math

                    height += Mathf.Lerp(chunkBiome.height, adjacentBiomes[i].height, weight);
                    startHeight += Mathf.Lerp(chunkBiome.startHeight, adjacentBiomes[i].startHeight, weight);
                    noiseScale += Mathf.Lerp(chunkBiome.noiseScale, adjacentBiomes[i].noiseScale, weight);
                    octaves += Mathf.Lerp(chunkBiome.octaves, adjacentBiomes[i].octaves, weight);
                    persistence += Mathf.Lerp(chunkBiome.persistence, adjacentBiomes[i].persistence, weight);
                    lacunarity += Mathf.Lerp(chunkBiome.lacunarity, adjacentBiomes[i].lacunarity, weight);
                    noiseCutoff += Mathf.Lerp(chunkBiome.noiseCutoff, adjacentBiomes[i].noiseCutoff, weight);

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
            noiseCutoff /= division;

            #endregion
        }

        else
        {
            height = chunkBiome.height;
            startHeight = chunkBiome.startHeight;
            noiseScale = chunkBiome.noiseScale;
            octaves = chunkBiome.octaves;
            persistence = chunkBiome.persistence;
            lacunarity = chunkBiome.lacunarity;
            noiseCutoff = chunkBiome.noiseCutoff;
        }

        #endregion

        #region Terrain

        //  Gets the terrainheigt through layered perlin noise, with noise values depending on above

        int terrainheight;
        int yPos = Mathf.FloorToInt(pos.y);
        terrainheight = Mathf.FloorToInt(Mathf.FloorToInt(height) * Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, noiseScale, Mathf.FloorToInt(octaves), persistence, lacunarity, noiseCutoff)) + Mathf.FloorToInt(startHeight);

        #endregion

        #region Falloff

        //  Terrain thats "falloff" worldsize from spawn, starts to get multiplied by a value aproaching 0,
        //  Meaning terrain thats approaching world border aproaches a height of 0
        //  This makes it so edges of world always are water, ie world surrounded by water

        if (Noise.GetFallOffMap(new Vector2(pos.x, pos.z)) < world.falloff)
        {
            terrainheight = Mathf.FloorToInt((1 / world.falloff) * terrainheight * Noise.GetFallOffMap(new Vector2(pos.x, pos.z)));
        }

        #endregion

        #region BlockType
        //  Determines the blocktype according to parameters

        if (yPos == terrainheight) return chunkBiome.surfaceBlockId;
        else if (yPos < terrainheight && yPos > terrainheight - 4) return chunkBiome.subSurfaceBlockId;
        else if (yPos < terrainheight) return 3;
        else if (yPos < 64) return 9;
        else return 0;

        #endregion
    }

    #region Blend

    private static float GetBiomeWeight(Tuple<int, int> VoxelPos, Tuple<int, int> biomePos)
    {
        //  Gets the weight the voxel inside of chunk biome and adjacent biome will blend with
        //  Gets the distance from voxel to point where adjacent biome is


        //  Stores coordinates in tuples, item1 is x, and item2 is z
        //  Coordinates are between 1 and 16
        //  If a coordinate is 0 its basically null and we ignore

        if (biomePos.Item1 != 0 && biomePos.Item2 != 0) //  If corners
        {
            float xWeight = (float)(VoxelData.chunkWidth - Mathf.Abs(biomePos.Item1 - VoxelPos.Item1)) / VoxelData.chunkWidth;
            float zWeight = (float)(VoxelData.chunkWidth - Mathf.Abs(biomePos.Item2 - VoxelPos.Item2)) / VoxelData.chunkWidth;

            return xWeight * zWeight;
        }

        else if (biomePos.Item1 != 0) //    If x != 0, which means z is 0, so we only care for x 
        {
            float xWeight = (float)(VoxelData.chunkWidth - Mathf.Abs(biomePos.Item1 - VoxelPos.Item1)) / VoxelData.chunkWidth;
            return xWeight;
        }

        else if (biomePos.Item2 != 0) //    If z != 0, which means x is 0, so we only care for z
        {
            float zWeight = (float)(VoxelData.chunkWidth - Mathf.Abs(biomePos.Item2 - VoxelPos.Item2)) / VoxelData.chunkWidth;
            return zWeight;
        }

        else // Default
        {
            return 1f;
        }     
    }

    private static Tuple<int, int> GetPositionInChunk(ChunkCoordinate cc, Vector3 pos)
    {
        //  Takes worldposition of voxel minus world postion of chunk
        //  Voxels position is always local position larger, so we end up with local postion

        int x = Mathf.FloorToInt(pos.x) - (cc.x * VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z) - (cc.z * VoxelData.chunkWidth);

        return Tuple.Create(x + 1, z + 1);
    }

    private static Biome[] GetAdjacentBiomes(World world, ChunkCoordinate cc)
    {
        //  The coordinates of adjacent chunks according to chunk, all values are 1 bigger than the should,
        //  because biome map has an extra layer
        //  We need middle chunk so we have a reference to what biome we are

        Biome[] biomes = new Biome[9]
        {
            world.biomeMap[cc.x + 2, cc.z + 2], //  top right
            world.biomeMap[cc.x + 1, cc.z + 2], //  top
            world.biomeMap[cc.x, cc.z + 2], //  top left
            world.biomeMap[cc.x + 2, cc.z + 1], //  right
            world.biomeMap[cc.x + 1, cc.z + 1], // middle
            world.biomeMap[cc.x, cc.z + 1], //  left
            world.biomeMap[cc.x + 2, cc.z], //  bottom right
            world.biomeMap[cc.x + 1, cc.z], //  bottom
            world.biomeMap[cc.x, cc.z] //   bottom left
        };

        return biomes;
    }

    private static readonly int[,] BiomePositions = new int[9, 2]
    {
        //  The local position of adjacent chunk,

        {VoxelData.chunkWidth, VoxelData.chunkWidth}, //    top right
        {0, VoxelData.chunkWidth}, //   top
        {1, VoxelData.chunkWidth}, //   top left
        {VoxelData.chunkWidth, 0}, //   right
        {0, 0}, //  middle
        {1, 0}, //  left
        {VoxelData.chunkWidth, 1}, //   bottom right
        {0, 1}, //  bottom
        {1, 1} //   bottom left
    };

    private static bool IsBlend(Biome[] adjacentBiomes, Biome chunkBiome)
    {
        //  We blend if unique adjacent biomes is larger than 1

        if(adjacentBiomes.Distinct().ToArray().Length > 1)
        {
            return true;
        }

        return false;   
    }

    #endregion

    private static bool IsVoxelInWorld(Vector3 pos)
    {
        //  Checks if voxel is inside world

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxel && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxel)
            return true;
        else
            return false;
    }
}
