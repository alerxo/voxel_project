using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WorldTerrain
{
    public static byte GetVoxelBlockType(World world, Vector3 pos, Biome _blendBiome, int _terrainHeight, bool blend)
    {
        //  Goes through different parameters to determine blocktype

        ChunkCoordinate cc = world.GetChunkCoordinateFromVector3(pos);

        #region Constants

        //  If outside of world, block is air

        if (!IsVoxelInWorld(pos))
        {
            //  If outside world, return air
            return 0;
        }

        #endregion

        #region Biome

        //  Gets the biome from biome map

        Biome chunkBiome = world.biomeMap[cc.x + 1, cc.z + 1];

        #endregion

        #region Terrain

        //  If we don't have a cached value for terrainheight, calculate terrainheight

        int terrainHeight;
        int yPos = Mathf.FloorToInt(pos.y);

        if (_blendBiome == null)
        {
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

            //  Gets voxel positon in chunk, and checks if we should blend the chunk with adjacent chunks

            int xPos = GetPositionInChunk(cc, pos).Item1;
            int zPos = GetPositionInChunk(cc, pos).Item2;

            if (IsBlend(adjacentBiomes, blend))
            {
                float[] blendNoiseValues = GetBlendNoiseValues(adjacentBiomes, world.GetChunkFromVector3(pos), xPos, zPos);
                float[] averageNoiseAtCenter = GetAverageNoiseAtCenter(adjacentBiomes, world.GetChunkFromVector3(pos));

                Tuple<float, Biome> blendValues = GetBlendValues(world, pos, adjacentBiomes, xPos, zPos);

                float weight = blendValues.Item1;
                chunkBiome = blendValues.Item2;


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

            //  Gets the terrainheigt through layered perlin noise, with noise values depending on above

            terrainHeight = Mathf.FloorToInt(Mathf.FloorToInt(height) * Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, noiseScale, Mathf.FloorToInt(octaves), persistence, lacunarity, noiseCutoff)) + Mathf.FloorToInt(startHeight);

            #region Falloff

            //  Terrain thats "falloff" worldsize from spawn, starts to get multiplied by a value aproaching 0,
            //  Meaning terrain thats approaching world border aproaches a height of 0
            //  This makes it so edges of world always are water, ie world surrounded by water

            float noise = Noise.GetFallOffMap(new Vector2(pos.x, pos.z));

            float falloff = (float)VoxelData.mapEdgeSizeInChunks / (float)VoxelData.worldSizeInChunks;

            if (noise < falloff)
            {
                float falloffValue = noise / falloff;

                terrainHeight = Mathf.FloorToInt(terrainHeight * falloffValue);
            }

            //  Chaching the height

            world.chunksInWorld[cc.x, cc.z].tempTerrainHeight = terrainHeight;
            world.chunksInWorld[cc.x, cc.z].tempBlendBiome = chunkBiome;

            #endregion
        }

        else
        {
            terrainHeight = _terrainHeight;
            chunkBiome = _blendBiome;
        }

        #endregion

        #region BlockType

        //  Determines the blocktype according to parameters

        if (yPos == terrainHeight) return chunkBiome.surfaceBlockId;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4) return chunkBiome.subSurfaceBlockId;
        else if (yPos < terrainHeight) return 3;
        else if (yPos < 64) return 8;
        else return 0;

        #endregion
    }

    #region Weight

    private static Tuple<float, Biome> GetBlendValues(World world, Vector3 pos, Biome[] adjacentBiomes, int voxelX, int voxelZ)
    {
        //  Chunk is divided into 4 parts, depending on where voxel is inside chunk, use different calculations
        //  Gets the distance from voxel to edge of chunk part

        int a = VoxelData.chunkWidth / 2;
        int b = (VoxelData.chunkWidth / 2) + 1;

        if (voxelX <= 8 && voxelZ <= 8) //    bottom left corner
        {
            return GetBlend(world, pos, adjacentBiomes, b - voxelX, b - voxelZ, 5, 8, 7);
        }

        else if (voxelX > 8 && voxelZ <= 8) //   bottom right corner
        {
            return GetBlend(world, pos, adjacentBiomes, voxelX - a, b - voxelZ, 3, 6, 7);
        }

        else if (voxelX <= 8 && voxelZ > 8) //     top left corner
        {
            return GetBlend(world, pos, adjacentBiomes, b - voxelX, voxelZ - a, 5, 2, 1);
        }

        else //    top right corner
        {
            return GetBlend(world, pos, adjacentBiomes, voxelX - a, voxelZ - a, 3, 0, 1);
        }
    }

    private static Tuple<float, Biome> GetBlend(World world, Vector3 pos, Biome[] adjacentBiomes, int x, int z, int biomeX, int corner, int biomeZ)
    {
        //  We only blend towards biomes that arent chunk biome
        //  Weight is distance from voxel pos in chunk part, to average biome pos

        float a = VoxelData.chunkWidth / 2;

        Vector2 xPos = new(a, 1);
        Vector2 zPos = new(1, a);
        Vector2 cornerPos = new(a, a);

        Vector2 voxelPos = new(x, z);

        bool[] adjacentBlendStates = GetAdjacentBlendStates(adjacentBiomes);

        float distance = a;

        Biome biome;

        #region nasty

        if (!adjacentBlendStates[biomeX] && adjacentBlendStates[biomeZ]) // If adjacent biome is on the y/z axis, take distance from y/z edge
        {
            distance = MathF.Min(zPos.y - voxelPos.y, distance);
        }

        else if (adjacentBlendStates[biomeX] && !adjacentBlendStates[biomeZ]) // If adjacent biome is on the x axis, take distance from x edge
        {
            distance = MathF.Min(xPos.x - voxelPos.x, distance);
        }

        else if (!adjacentBlendStates[biomeX] && adjacentBlendStates[corner] && !adjacentBlendStates[biomeZ]) //  If adjacent biome is the corner, take distance from corner, (dived by 1.4 because pythagaros therorem)
        {
            distance = MathF.Min(Vector2.Distance(voxelPos, cornerPos) / 1.4f, distance);
        }

        else if (adjacentBlendStates[biomeX] && adjacentBlendStates[corner] && adjacentBlendStates[biomeZ]) //   If all 3 biomes are adjacent, take distance from the closest
        {
            distance = MathF.Min(Vector2.Distance(voxelPos, xPos), distance);
            distance = MathF.Min(Vector2.Distance(voxelPos, cornerPos), distance);
            distance = MathF.Min(Vector2.Distance(voxelPos, zPos), distance);
        }

        else if (!adjacentBlendStates[biomeX] && !adjacentBlendStates[corner] && !adjacentBlendStates[biomeZ]) //    If no adjacent
        {
            //  look for an adjacent to entire chunk

            if ((adjacentBlendStates[3] || adjacentBlendStates[5]) && !adjacentBlendStates[1] && !adjacentBlendStates[7]) //    if the adjacent is left/right, ie x axis, take distance from x edge
            {
                distance = MathF.Min(xPos.x - voxelPos.x, distance);
            }

            else if ((adjacentBlendStates[1] || adjacentBlendStates[7]) && !adjacentBlendStates[3] && !adjacentBlendStates[5]) //   if the adjacent is top/bottom, ie y/z axis, take distance from y/z edge
            {
                distance = MathF.Min(zPos.y - voxelPos.y, distance);
            }

            else if ((adjacentBlendStates[0] || adjacentBlendStates[2] || adjacentBlendStates[6] || adjacentBlendStates[8]))
            {
                distance = MathF.Min(Vector2.Distance(voxelPos, cornerPos), distance);
            }
        }

        #endregion

        float noise = Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, 5f, 1, 0.5f, 2f, 1f);

        #region nasty number 2

        if (noise < 0.15f)
        {
            biome = adjacentBiomes[biomeX];
        }

        else if(noise < 0.30f)
        {
            biome = adjacentBiomes[corner];
        }

        else if(noise < 0.45f)
        {
            biome = adjacentBiomes[biomeZ];
        }

        else
        {
            biome = adjacentBiomes[4];
        }

        #endregion


        return Tuple.Create((1f - (distance / a)) * (1 - (noise * noise)), biome);
    }

    #endregion

    #region Noise

    private static float[] GetBlendNoiseValues(Biome[] adjacentBiomes, Chunk chunk, int voxelX, int voxelZ)
    {
        //  Gets the average noise values of adjacent chunks, depending on which part of chunk we are in

        int a = VoxelData.chunkWidth / 2;

        if (voxelX <= a && voxelZ <= a) //   bottom right corner
        {
            if (chunk.bottomRightCornerNoise == null)
            {
                chunk.bottomRightCornerNoise = GetAverageNoiseAtCorner(new Biome[]
                {
                    adjacentBiomes[8],
                    adjacentBiomes[7],
                    adjacentBiomes[5],
                    adjacentBiomes[4]
                });
            }

            return chunk.bottomRightCornerNoise;
        }

        else if (voxelX <= a && voxelZ > a) //    top right corner
        {
            if (chunk.topRightCornerNoise == null)
            {
                chunk.topRightCornerNoise = GetAverageNoiseAtCorner(new Biome[]
                {
                    adjacentBiomes[5],
                    adjacentBiomes[4],
                    adjacentBiomes[2],
                    adjacentBiomes[1]

                });
            }

            return chunk.topRightCornerNoise;
        }

        else if (voxelX > a && voxelZ <= a) //    bottom left corner
        {
            if (chunk.bottomLeftCornerNoise == null)
            {
                chunk.bottomLeftCornerNoise = GetAverageNoiseAtCorner(new Biome[]
                {
                    adjacentBiomes[7],
                    adjacentBiomes[6],
                    adjacentBiomes[4],
                    adjacentBiomes[3]

                });
            }

            return chunk.bottomLeftCornerNoise;
        }

        else //     top left corner
        {
            if (chunk.topLeftCornerNoise == null)
            {
                chunk.topLeftCornerNoise = GetAverageNoiseAtCorner(new Biome[]
                {
                    adjacentBiomes[4],
                    adjacentBiomes[3],
                    adjacentBiomes[1],
                    adjacentBiomes[0]

                });
            }

            return chunk.topLeftCornerNoise;
        }
    }

    private static float[] GetAverageNoiseAtCorner(Biome[] biomes)
    {
        //  Gets the average noise at a corner of a chunk
        //  Loops through each biome

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

        List<Biome> duplicates = new List<Biome>();

        int amount = 0;

        for (int i = 0; i < 4; i++)
        {
            if (!duplicates.Contains(biomes[i]))
            {
                duplicates.Add(biomes[i]);

                amount++;

                blendNoiseValues[0] += biomes[i].height;
                blendNoiseValues[1] += biomes[i].startHeight;
                blendNoiseValues[2] += biomes[i].noiseScale;
                blendNoiseValues[3] += biomes[i].octaves;
                blendNoiseValues[4] += biomes[i].persistence;
                blendNoiseValues[5] += biomes[i].lacunarity;
                blendNoiseValues[6] += biomes[i].noiseCutoff;
            }
        }

        blendNoiseValues[0] /= amount;
        blendNoiseValues[1] /= amount;
        blendNoiseValues[2] /= amount;
        blendNoiseValues[3] /= amount;
        blendNoiseValues[4] /= amount;
        blendNoiseValues[5] /= amount;
        blendNoiseValues[6] /= amount;

        return blendNoiseValues.ToArray();
    }

    private static float[] GetAverageNoiseAtCenter(Biome[] biomes, Chunk chunk)
    {
        //  Gets average noise at center of chunk
        //  Loops through each biome

        int a = VoxelData.chunkWidth;

        if (chunk.centerNoise == null)
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
                    float[] averageNoiseAtCorner = GetBlendNoiseValues(biomes, chunk, x * a, z * a);

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

            chunk.centerNoise = blendNoiseValues.ToArray();
        }

        return chunk.centerNoise;
    }

    #endregion

    #region Adjacent biomes

    private static Biome[] GetAdjacentBiomes(World world, ChunkCoordinate cc)
    {
        //  The coordinates of adjacent chunks according to chunk, all values are 1 bigger than the should,
        //  because biome map has an extra layer
        //  We need middle chunk so we have a reference to what biome we are

        Biome[] adjacentBiomes = new Biome[9]
        {
            world.biomeMap[cc.x + 2, cc.z + 2], //  top right 0
            world.biomeMap[cc.x + 1, cc.z + 2], //  top 1
            world.biomeMap[cc.x, cc.z + 2], //  top left 2
            world.biomeMap[cc.x + 2, cc.z + 1], //  right 3
            world.biomeMap[cc.x + 1, cc.z + 1], // middle 4
            world.biomeMap[cc.x, cc.z + 1], //  left 5
            world.biomeMap[cc.x + 2, cc.z], //  bottom right 6
            world.biomeMap[cc.x + 1, cc.z], //  bottom 7
            world.biomeMap[cc.x, cc.z] //   bottom left 8
        };

        return adjacentBiomes;
    }

    private static bool[] GetAdjacentBlendStates(Biome[] adjacentBiomes)
    {
        //  Checks which adjacent chunks are of a different biome than chunk

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

    #endregion

    private static Tuple<int, int> GetPositionInChunk(ChunkCoordinate cc, Vector3 pos)
    {
        //  Takes worldposition of voxel minus world postion of chunk
        //  Voxels position is always local position larger, so we end up with local postion

        int x = Mathf.FloorToInt(pos.x) - (cc.x * VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z) - (cc.z * VoxelData.chunkWidth);

        return Tuple.Create(x + 1, z + 1);
    }

    private static bool IsBlend(Biome[] adjacentBiomes, bool blend)
    {
        //  We blend if unique adjacent biomes is larger than 1

        if (!blend || adjacentBiomes.Distinct().ToArray().Length > 1)
        {
            return true;
        }

        return false;
    }

    private static bool IsVoxelInWorld(Vector3 pos)
    {
        //  Checks if voxel is inside world

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxel && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxel)
            return true;
        else
            return false;
    }
}