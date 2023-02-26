using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TerrainHeight
{
    public static int GetTerrainHeight(World world, Vector2 position)
    {
        ChunkCoordinate cc = Voxel_Util.GetChunkCoordinateFromVector3(new Vector3(position.x, 1, position.y));
        SubBiome subBiome = world.chunksInWorld[cc.x + 1, cc.z + 1].subBiome;
        SubBiome[] adjacentBiomes = GetAdjacentBiomes(world, cc);

        if (subBiome == world.riverReference)
        {
            return RiverWaterfallBlend(world, cc, position, adjacentBiomes); 
        }

        int worldHeight = GetWorldHeight(world, position);
        int biomeHeight = GetBiomeHeight(world, position, subBiome.parentBiome);
        int subBiomeHeight = GetSubBiomeHeight(world, position, subBiome);

        if (IsBlend(adjacentBiomes))
        {
            //  Starts blending chunks sides where the adjacent biome is not same as chunk biome
            //  Does this by multiplying biomeheight with a value approaching 0, depending on distance
            //  So on the chunk border, only the world height applies, giving smooth blend

            biomeHeight = Mathf.FloorToInt((float)biomeHeight * BiomeBlend(adjacentBiomes, cc, position));
            subBiomeHeight = Mathf.FloorToInt((float)subBiomeHeight * SubBiomeBlend(adjacentBiomes, cc, position));
        }

        return worldHeight + biomeHeight + subBiomeHeight;
    }

    private static int GetWorldHeight(World world, Vector2 pos)
    {
        //  The global noise

        return Noise.Get2DPerlinWorld(world, pos);
    }

    private static int GetBiomeHeight(World world, Vector2 pos, Biome biome)
    {
        //  The biome noise

        return Mathf.FloorToInt(biome.height * Noise.Get2DPerlin(world, pos, 0, biome.noiseScale, biome.octaves, biome.persistence, biome.lacunarity, biome.noiseCutoff));
    }

    private static int GetSubBiomeHeight(World world, Vector2 pos, SubBiome subBiome)
    {
        //  The subbiome noise

        return Mathf.FloorToInt(subBiome.height * Noise.Get2DPerlin(world, pos, 0, subBiome.noiseScale, subBiome.octaves, subBiome.persistence, subBiome.lacunarity, subBiome.noiseCutoff));
    }

    public static int GetRiverHeight(World world, ChunkCoordinate cc)
    {
        //  Loops through enire chunk checking for lowest world height, and uses that as river height, so we have smooth surface

        int xPos = cc.x;
        int zPos = cc.z;

        Chunk chunk = world.chunksInWorld[xPos, zPos];

        if (chunk != null && chunk.riverWaterHeight != 0)
        {
            return world.chunksInWorld[xPos, zPos].riverWaterHeight;
        }

        int chunkWidth = VoxelData.chunkWidth;
        int terrainHeight = VoxelData.chunkHeight;

        for (int x = 0; x < chunkWidth; x++)
        {
            for (int z = 0; z < chunkWidth; z++)
            {
                terrainHeight = Mathf.Min(terrainHeight, GetWorldHeight(world, new Vector2((xPos * chunkWidth) + x, (zPos * chunkWidth) + z)));
            }
        }

        if (chunk != null)
        {
            chunk.riverWaterHeight = terrainHeight;
        }

        return terrainHeight;
    }


    public static bool IsBlend(SubBiome[] adjacentBiomes)
    {
        //  We check if there are any adjacent biomes not equal to chunkbiome,
        //  if there is only one unique biome in list, all 9 are the same, and we dont blend

        if (adjacentBiomes.Distinct().ToArray().Length > 1)
        {
            return true;
        }

        return false;
    }

    private static float SubBiomeBlend(SubBiome[] adjacentBiomes, ChunkCoordinate cc, Vector2 pos)
    {
        //  North, east, south, west

        //  Gets the voxels postion in chunk
        //  Then checks which is the closes adjacent chunk, where biome is different from chunk biome
        //  Returns the closest adjacent chunk blend, ie, the lowest value of the parameters

        float chunkWidth = VoxelData.chunkWidth;

        Tuple<int, int> positionInChunk = GetPositionInChunk(cc, pos);
        float x = positionInChunk.Item1;
        float y = positionInChunk.Item2;

        List<bool> ab = new List<bool>();
        foreach (SubBiome subBiome in adjacentBiomes)
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

        float blendDistance = adjacentBiomes[4].blendDistance;
        float blendValue = 1;

        #region math

        if (ab[0])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(chunkWidth, chunkWidth), new Vector2(x, y)) / chunkWidth));
        } //    top right

        if (ab[1])
        {
            blendValue = Mathf.Min(blendValue, ((chunkWidth - y) / chunkWidth));
        } //    top

        if (ab[2])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(0, chunkWidth), new Vector2(x, y)) / chunkWidth));
        } //    top left

        if (ab[3])
        {
            blendValue = Mathf.Min(blendValue, ((chunkWidth - x) / chunkWidth));
        } //    right

        if (ab[5])
        {
            blendValue = Mathf.Min(blendValue, (x / chunkWidth));
        } //    left        

        if (ab[6])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(chunkWidth, 0), new Vector2(x, y)) / chunkWidth));
        } //    bottom right

        if (ab[7])
        {
            blendValue = Mathf.Min(blendValue, (y / chunkWidth));
        } //    bottom

        if (ab[8])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(0, 0), new Vector2(x, y)) / chunkWidth));
        } //    bottom left 

        #endregion

        //  Adds noise as the remove repetetiveness

        if (blendValue >= blendDistance)
        {
            return 1;
        }

        else
        {
            blendValue /= blendDistance;

            return blendValue;
        }
    }

    private static float BiomeBlend(SubBiome[] adjacentBiomes, ChunkCoordinate cc, Vector2 pos)
    {
        //  North, east, south, west

        //  Gets the voxels postion in chunk
        //  Then checks which is the closes adjacent chunk, where biome is different from chunk biome
        //  Returns the closest adjacent chunk blend, ie, the lowest value of the parameters

        float chunkWidth = VoxelData.chunkWidth;

        Tuple<int, int> positionInChunk = GetPositionInChunk(cc, pos);
        float x = positionInChunk.Item1;
        float y = positionInChunk.Item2;

        List<bool> ab = new List<bool>();
        foreach (SubBiome subBiome in adjacentBiomes)
        {
            if (subBiome.parentBiome != adjacentBiomes[4].parentBiome)
            {
                ab.Add(true);
            }

            else
            {
                ab.Add(false);
            }
        }

        float blendDistance = adjacentBiomes[4].parentBiome.blendDistance;
        float blendValue = 1;

        #region math

        if (ab[0])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(chunkWidth, chunkWidth), new Vector2(x, y)) / chunkWidth));
        } //    top right

        if (ab[1])
        {
            blendValue = Mathf.Min(blendValue, ((chunkWidth - y) / chunkWidth));
        } //    top

        if (ab[2])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(0, chunkWidth), new Vector2(x, y)) / chunkWidth));
        } //    top left

        if (ab[3])
        {
            blendValue = Mathf.Min(blendValue, ((chunkWidth - x) / chunkWidth));
        } //    right

        if (ab[5])
        {
            blendValue = Mathf.Min(blendValue, (x / chunkWidth));
        } //    left        

        if (ab[6])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(chunkWidth, 0), new Vector2(x, y)) / chunkWidth));
        } //    bottom right

        if (ab[7])
        {
            blendValue = Mathf.Min(blendValue, (y / chunkWidth));
        } //    bottom

        if (ab[8])
        {
            blendValue = Mathf.Min(blendValue, (Vector2.Distance(new Vector2(0, 0), new Vector2(x, y)) / chunkWidth));
        } //    bottom left 

        #endregion


        if (blendValue >= blendDistance)
        {
            return 1;
        }

        else
        {
            blendValue /= blendDistance;

            return blendValue;
        }
    }

    private static int RiverWaterfallBlend(World world, ChunkCoordinate cc, Vector2 pos, SubBiome[] adjacentBiomes)
    {
        //  If have an adjacent river, we want to create a waterfall if that river is lower in height,
        //  to create smooth transitions between river chunks
        //  Checks if voxel is on the border

        int riverHeight = GetRiverHeight(world, cc);
        int blendRiver = VoxelData.chunkHeight;

        bool[] adjacentRivers = GetAdjacentRivers(adjacentBiomes);

        Tuple<int, int> pic = GetPositionInChunk(cc, pos);

        for (int i = 0; i < 4; i++)
        {
            Tuple<int, int> rpic = GetBorderVoxels(i);

            if ((pic.Item1 == rpic.Item1 || pic.Item2 == rpic.Item2) && adjacentRivers[i])
            {
                Tuple<int, int> arp = GetAdjacentRiverPos(i);

                blendRiver = Mathf.Min(blendRiver, GetRiverHeight(world, new ChunkCoordinate(cc.x + arp.Item1, cc.z + arp.Item2)));
            }
        }

        return Mathf.Min(riverHeight, blendRiver);
    }

    private static Tuple<int, int> GetBorderVoxels(int i)
    {
        //  Lookup table for which voxels in chunk are to be blended,
        //  for example, if river is to the south, we want the southern border of voxels, ie all voxels where y = 1
        //  -1 means we disregard, null or something like that

        int cw = VoxelData.chunkWidth;

        Tuple<int, int>[] rightPosInChunk = new Tuple<int, int>[4]
        {
            Tuple.Create(-1, cw), //    North
            Tuple.Create(cw, -1), //    East
            Tuple.Create(1, -1), //     West
            Tuple.Create(-1, 1), //     South
        };

        return rightPosInChunk[i];
    }

    private static Tuple<int, int> GetAdjacentRiverPos(int i)
    {
        //  Gets position of adjacent chunks depending on index i

        Tuple<int, int>[] adjacentRiverPos = new Tuple<int, int>[4]
        {
            Tuple.Create(0, 1), //  North
            Tuple.Create(1, 0), //  East
            Tuple.Create(-1, 0), // West
            Tuple.Create(0, -1), // South
        };

        return adjacentRiverPos[i];
    }


    public static SubBiome[] GetAdjacentBiomes(World world, ChunkCoordinate cc)
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

    private static Tuple<int, int> GetPositionInChunk(ChunkCoordinate cc, Vector2 pos)
    {
        //  Takes worldposition of voxel minus world postion of chunk
        //  Voxels position is always local position larger, so we end up with local postion

        int x = Mathf.FloorToInt(pos.x) - (cc.x * VoxelData.chunkWidth);
        int y = Mathf.FloorToInt(pos.y) - (cc.z * VoxelData.chunkWidth);

        return Tuple.Create(x + 1, y + 1);
    }

    private static bool[] GetAdjacentRivers(SubBiome[] adjacentBiomes)
    {
        //  Creates a list of bools, if the adjacent chunk is river, then that postion in list is true

        bool[] adjacentRivers = new bool[4];

        if (adjacentBiomes[1] == adjacentBiomes[4])
        {
            adjacentRivers[0] = true;
        }

        else if (adjacentBiomes[3] == adjacentBiomes[4])
        {
            adjacentRivers[1] = true;
        }

        else if (adjacentBiomes[5] == adjacentBiomes[4])
        {
            adjacentRivers[2] = true;
        }

        else if (adjacentBiomes[7] == adjacentBiomes[4])
        {
            adjacentRivers[3] = true;
        }


        return adjacentRivers;
    }
}