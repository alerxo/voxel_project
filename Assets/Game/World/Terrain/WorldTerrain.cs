using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WorldTerrain
{
    public static byte GetVoxelBlockType(World world, Vector3 pos, bool isBlend, int cachedWorldHeight, int cachedBiomeHeight, SubBiome cachedBlendBiome)
    {
        //  Goes through different parameters to determine blocktype

        ChunkCoordinate cc = Voxel_Util.GetChunkCoordinateFromVector3(pos);

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

        SubBiome chunkBiome = world.subBiomeMap[cc.x + 1, cc.z + 1];
        bool isRiver = world.riverMap[cc.x + 1, cc.z + 1];

        #endregion

        #region Terrain

        int worldHeight;
        int biomeHeight;

        if (cachedWorldHeight == 0)
        {
            worldHeight = GetWorldHeight(world, pos);
            biomeHeight = GetBiomeHeight(world, pos, chunkBiome, isRiver);

            //  Blend

            SubBiome[] adjacentBiomes = GetAdjacentBiomes(world, cc);
            bool[] adjacentRivers = GetAdjacentRivers(world, cc);

            if (IsBlend(isBlend, adjacentBiomes, adjacentRivers))
            {
                //  Starts blending chunks sides where the adjacent biome is not same as chunk biome
                //  Does this by multiplying biomeheight with a value approaching 0, depending on distance
                //  So on the chunk border, only the world height applies, giving smooth blend



                biomeHeight = Mathf.FloorToInt((float)biomeHeight * TerrainBlend(world, adjacentBiomes, adjacentRivers, cc, pos));

                chunkBiome = BiomeBlend(world, new Vector2(pos.x, pos.z), adjacentBiomes);
            }

            world.chunksInWorld[cc.x, cc.z].cachedWorldHeight = worldHeight;
            world.chunksInWorld[cc.x, cc.z].cachedBiomeHeight = biomeHeight;
            world.chunksInWorld[cc.x, cc.z].cachedBlendBiome = chunkBiome;
        }

        else
        {
            worldHeight = cachedWorldHeight;
            biomeHeight = cachedBiomeHeight;
            chunkBiome = cachedBlendBiome;
        }

        int riverHeight = GetRiverHeight(world, cc, pos, isRiver, chunkBiome);
        int terrainHeight = GetTerrainHeight(worldHeight, biomeHeight, riverHeight, isRiver);

        int yPos = Mathf.FloorToInt(pos.y);

        #endregion

        #region BlockType

        //  Determines the blocktype according to parameters

        if (yPos == terrainHeight) return chunkBiome.surfaceBlockId;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4) return chunkBiome.subSurfaceBlockId;
        else if (yPos < terrainHeight) return 3;

        else if (isRiver && yPos <= riverHeight) return 8;

        else if (yPos < 64) return 8;
        else return 0;

        #endregion
    }

    private static int GetWorldHeight(World world, Vector3 pos)
    {
        int worldHeight;

        worldHeight = Mathf.FloorToInt(128f * Noise.Get2DPerlinHighResolution(world, world.resolution, new Vector2(pos.x, pos.z), 0f, world.noiseScale, world.octaves, world.persistence, world.lacunarity, world.noiseCutoff) * Noise.GetFallOffMap(new Vector2(pos.x, pos.z)));

        return worldHeight;
    }

    private static int GetBiomeHeight(World world, Vector3 pos, SubBiome chunkBiome, bool isRiver)
    {
        int biomeHeight;

        biomeHeight = Mathf.FloorToInt(chunkBiome.height * Noise.Get2DPerlinHighResolution(world, world.resolution, new Vector2(pos.x, pos.z), 0, chunkBiome.noiseScale, chunkBiome.octaves, chunkBiome.persistence, chunkBiome.lacunarity, chunkBiome.noiseCutoff));

        if (isRiver)
        {
            biomeHeight += world.riverReference.height;
        }

        return biomeHeight;
    }

    private static int GetTerrainHeight(int worldHeight, int biomeHeight, int riverHeight, bool isRiver)
    {
        int terrainHeight = worldHeight + biomeHeight;

        if (isRiver && terrainHeight >= riverHeight)
        {
            terrainHeight = riverHeight - 1;
        }

        return terrainHeight;
    }

    private static int GetRiverHeight(World world, ChunkCoordinate cc, Vector3 pos, bool isRiver, SubBiome subBiome)
    {
        if (!isRiver) return 0;

        int riverHeight;

        if (world.chunksInWorld[cc.x, cc.z].riverWaterHeight == 0)
        {
            riverHeight = GetTerrainHeight(GetWorldHeight(world, pos), GetBiomeHeight(world, pos, subBiome, true), 0, false);


            //  Temp
            riverHeight = GetWorldHeight(world, pos);

            world.chunksInWorld[cc.x, cc.z].riverWaterHeight = riverHeight;
        }

        else
        {
            riverHeight = world.chunksInWorld[cc.x, cc.z].riverWaterHeight;
        }

        return riverHeight;
    }


    private static bool IsBlend(bool isBlend, SubBiome[] adjacentBiomes, bool[] adjacentRivers)
    {
        //  We check if there are any adjacent biomes not equal to chunkbiome, or if there are any rivers

        if (isBlend && !adjacentBiomes.All(x => adjacentBiomes[4]))
        {
            return true;
        }

        else if(isBlend && !adjacentRivers.All(x => false))
        {
            return true;
        }

        return false;
    }

    private static float TerrainBlend(World world, SubBiome[] adjacentBiomes, bool[] adjacentRivers, ChunkCoordinate cc, Vector3 pos)
    {
        //  North, east, south, west

        //  Gets the voxels postion in chunk
        //  Then checks which is the closes adjacent chunk, where biome is different from chunk biome
        //  Returns the closest adjacent chunk blend, ie, the lowest value of the parameters

        float chunkWidth = VoxelData.chunkWidth;

        Tuple<int, int> positionInChunk = GetPositionInChunk(cc, pos);
        
        float x = positionInChunk.Item1;
        float y = positionInChunk.Item2;

        #region math

        List<bool> ab = new List<bool>();
        foreach(SubBiome subBiome in adjacentBiomes)
        {
            if (subBiome != adjacentBiomes[4])
            {
                ab.Add(true);
            }

            else
            {
                ab.Add(false);
            }
        }

        float noise = Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, 10, 4, 0.5f, 2f, 1);

        float distanceNoise = (1 + noise) / 2;

        float blendValue = 1;

        if (ab[0] || adjacentRivers[0])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(16, 16), new Vector2(x, y)) / chunkWidth) * distanceNoise);
        } //    top right

        if (ab[1] || adjacentRivers[1])
        {
            blendValue = Mathf.Min(blendValue, ((chunkWidth - y) / chunkWidth) * distanceNoise);
        } //    top

        if (ab[2] || adjacentRivers[2])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(0, 16) * noise, new Vector2(x, y)) / chunkWidth) * distanceNoise);
        } //    top left

        if (ab[3] || adjacentRivers[3])
        {
            blendValue = Mathf.Min(blendValue, ((chunkWidth - x) / chunkWidth) * distanceNoise);
        } //    right

        if (ab[5] || adjacentRivers[5])
        {
            blendValue = Mathf.Min(blendValue, ((0 + x) / chunkWidth) * distanceNoise);
        } //    left    

        if (ab[6] || adjacentRivers[6])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(16, 0) * noise, new Vector2(x, y)) / chunkWidth) * distanceNoise);
        } //    bottom right

        if (ab[7] || adjacentRivers[7])
        {
            blendValue = Mathf.Min(blendValue, ((0 + y) / chunkWidth) * distanceNoise);
        } //    bottom

        if (ab[8] || adjacentRivers[8])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(0, 0) * noise, new Vector2(x, y)) / chunkWidth) * distanceNoise);
        } //    bottom left

        #endregion

        //  Adds noise as the remove repetetiveness

        return (blendValue + (noise * blendValue)) / 2;
    }

    private static SubBiome BiomeBlend(World world, Vector2 pos, SubBiome[] adjacentBiomes)
    {
        float noise = Noise.Get2DPerlin(world, pos, 0, 5f, 1, 0.5f, 2f, 1f);

        if (noise < 0.05f)
        {
            return adjacentBiomes[0];
        }

        else if (noise < 0.10f)
        {
            return adjacentBiomes[1];
        }

        else if (noise < 0.15f)
        {
            return adjacentBiomes[2];
        }

        else if (noise < 0.20f)
        {
            return adjacentBiomes[3];
        }

        else if (noise < 0.25f)
        {
            return adjacentBiomes[5];
        }

        else if (noise < 0.30f)
        {
            return adjacentBiomes[6];
        }

        else if (noise < 0.35f)
        {
            return adjacentBiomes[7];
        }

        else if (noise < 0.40f)
        {
            return adjacentBiomes[8];
        }

        else
        {
            return adjacentBiomes[4];
        }
    }

    private static SubBiome[] GetAdjacentBiomes(World world, ChunkCoordinate cc)
    {
        //  The coordinates of adjacent chunks according to chunk, all values are 1 bigger than the should,
        //  because biome map has an extra layer
        //  We need middle chunk so we have a reference to what biome we are

        SubBiome[] adjacentBiomes = new SubBiome[9]
        {
            world.subBiomeMap[cc.x + 2, cc.z + 2], //  top right 0
            world.subBiomeMap[cc.x + 1, cc.z + 2], //  top 1
            world.subBiomeMap[cc.x, cc.z + 2], //  top left 2
            world.subBiomeMap[cc.x + 2, cc.z + 1], //  right 3
            world.subBiomeMap[cc.x + 1, cc.z + 1], // middle 4
            world.subBiomeMap[cc.x, cc.z + 1], //  left 5
            world.subBiomeMap[cc.x + 2, cc.z], //  bottom right 6
            world.subBiomeMap[cc.x + 1, cc.z], //  bottom 7
            world.subBiomeMap[cc.x, cc.z] //   bottom left 8
        };

        return adjacentBiomes;
    }

    private static bool[] GetAdjacentRivers(World world, ChunkCoordinate cc)
    {
        //  -||-

        bool[] adjacentRivers = new bool[9]
        {
            world.riverMap[cc.x + 2, cc.z + 2], //  top right 0
            world.riverMap[cc.x + 1, cc.z + 2], //  top 1
            world.riverMap[cc.x, cc.z + 2], //  top left 2
            world.riverMap[cc.x + 2, cc.z + 1], //  right 3
            world.riverMap[cc.x + 1, cc.z + 1], // middle 4
            world.riverMap[cc.x, cc.z + 1], //  left 5
            world.riverMap[cc.x + 2, cc.z], //  bottom right 6
            world.riverMap[cc.x + 1, cc.z], //  bottom 7
            world.riverMap[cc.x, cc.z] //   bottom left 8
        };

        return adjacentRivers;
    }

    private static Tuple<int, int> GetPositionInChunk(ChunkCoordinate cc, Vector3 pos)
    {
        //  Takes worldposition of voxel minus world postion of chunk
        //  Voxels position is always local position larger, so we end up with local postion

        int x = Mathf.FloorToInt(pos.x) - (cc.x * VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z) - (cc.z * VoxelData.chunkWidth);

        return Tuple.Create(x + 1, z + 1);
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