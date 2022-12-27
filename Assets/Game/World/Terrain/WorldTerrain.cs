using System;
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
            float[] blendNoiseValues = GetBlendNoiseValues(adjacentBiomes, xPos, zPos);
            float[] averageNoiseAtCenter = GetAverageNoiseAtCenter(adjacentBiomes);

            float weight = GetBiomeWeight(adjacentBiomes, xPos, zPos);

            #region math

            height = Mathf.Lerp(averageNoiseAtCenter[0], blendNoiseValues[0], weight);
            startHeight = Mathf.Lerp(averageNoiseAtCenter[1], blendNoiseValues[1], weight);
            noiseScale = Mathf.Lerp(averageNoiseAtCenter[2], blendNoiseValues[2], weight);
            octaves = Mathf.Lerp(averageNoiseAtCenter[3], blendNoiseValues[3], weight);
            persistence = Mathf.Lerp(averageNoiseAtCenter[4], blendNoiseValues[4], weight);
            lacunarity = Mathf.Lerp(averageNoiseAtCenter[5], blendNoiseValues[5], weight);
            noiseCutoff = Mathf.Lerp(averageNoiseAtCenter[6], blendNoiseValues[6], weight);

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

    private static float GetBiomeWeight(Biome[] adjacentBiomes, int voxelX, int voxelZ)
    {
        bool[] adjacentBiomeBlendStates = GetAdjacentBlendStates(adjacentBiomes);

        if (voxelX <= 8 && voxelZ <= 8) //   bottom right corner
        {
            float xWeight = (float)(8 - Mathf.Abs(1 - voxelX)) / 8;
            float zWeight = (float)(8 - Mathf.Abs(1 - voxelZ)) / 8;

            return xWeight * zWeight;
        }

        else if (voxelX <= 8 && voxelZ > 8) //    top right corner
        {
            return 1f;
        }

        else if (voxelX > 8 && voxelZ <= 8) //    bottom left corner
        {
            return 1f;
        }

        else //     top left corner
        {
            return 1f;
        }
    }

    private static float[] GetBlendNoiseValues(Biome[] adjacentBiomes, int voxelX, int voxelZ)
    {
        if(voxelX <= 8 && voxelZ <= 8) //   bottom right corner
        {
            return GetAverageNoiseAtCorner(new Biome[]
            {
                adjacentBiomes[8],
                adjacentBiomes[7],
                adjacentBiomes[5],
                adjacentBiomes[4]
                
            });
        }

        else if(voxelX <= 8 && voxelZ > 8) //    top right corner
        {
            return GetAverageNoiseAtCorner(new Biome[]
            {
                adjacentBiomes[5],
                adjacentBiomes[4],
                adjacentBiomes[2],
                adjacentBiomes[1]

            });
        }

        else if(voxelX > 8 && voxelZ <= 8) //    bottom left corner
        {
            return GetAverageNoiseAtCorner(new Biome[]
            {
                adjacentBiomes[7],
                adjacentBiomes[6],
                adjacentBiomes[4],
                adjacentBiomes[3]

            });
        }

        else //     top left corner
        {
            return GetAverageNoiseAtCorner(new Biome[]
            {
                adjacentBiomes[4],
                adjacentBiomes[3],
                adjacentBiomes[1],
                adjacentBiomes[0]

            });
        }
    }

    private static float[] GetAverageNoiseAtCorner(Biome[] biomes)
    {
        List<float> blendNoiseValues = new List<float>
        {
            0,
            0,
            0,
            0,
            0,
            0,
            0
        };

        for (int i = 0; i < 4; i++)
        {
            blendNoiseValues[0] += biomes[i].height;
            blendNoiseValues[1] += biomes[i].startHeight;
            blendNoiseValues[2] += biomes[i].noiseScale;
            blendNoiseValues[3] += biomes[i].octaves;
            blendNoiseValues[4] += biomes[i].persistence;
            blendNoiseValues[5] += biomes[i].lacunarity;
            blendNoiseValues[6] += biomes[i].noiseCutoff;
        }

        blendNoiseValues[0] /= 4;
        blendNoiseValues[1] /= 4;
        blendNoiseValues[2] /= 4;
        blendNoiseValues[3] /= 4;
        blendNoiseValues[4] /= 4;
        blendNoiseValues[5] /= 4;
        blendNoiseValues[6] /= 4;

        return blendNoiseValues.ToArray();
    }

    private static float[] GetAverageNoiseAtCenter(Biome[] biomes)
    {
        List<float> blendNoiseValues = new List<float>
        {
            0,
            0,
            0,
            0,
            0,
            0,
            0
        };

        for (int x = 0; x < 2; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                float[] averageNoiseAtCorner = GetBlendNoiseValues(biomes, x * 16, z * 16);

                blendNoiseValues[0] += averageNoiseAtCorner[0];
                blendNoiseValues[1] += averageNoiseAtCorner[1];
                blendNoiseValues[2] += averageNoiseAtCorner[2];
                blendNoiseValues[3] += averageNoiseAtCorner[3];
                blendNoiseValues[4] += averageNoiseAtCorner[4];
                blendNoiseValues[5] += averageNoiseAtCorner[5];
                blendNoiseValues[6] += averageNoiseAtCorner[6];
            }

        }

        blendNoiseValues[0] /= 4;
        blendNoiseValues[1] /= 4;
        blendNoiseValues[2] /= 4;
        blendNoiseValues[3] /= 4;
        blendNoiseValues[4] /= 4;
        blendNoiseValues[5] /= 4;
        blendNoiseValues[6] /= 4;

        return blendNoiseValues.ToArray();
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

        Biome[] adjacentBiomes = new Biome[9]
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

        return adjacentBiomes;
    }

    private static bool[] GetAdjacentBlendStates(Biome[] adjacentBiomes)
    {
        List<bool> _adjacentBiomesBlend = new List<bool>();

        foreach (Biome biome in adjacentBiomes)
        {
            if (biome != adjacentBiomes[4])
            {
                _adjacentBiomesBlend.Add(true);
            }

            else
            {
                _adjacentBiomesBlend.Add(false);
            }
        }

        return _adjacentBiomesBlend.ToArray();
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