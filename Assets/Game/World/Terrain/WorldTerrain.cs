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

        Vector2 pos2d = new Vector2(pos.x, pos.z);

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
        bool isRiver = chunkBiome == world.riverReference;
        bool isCave = false;

        #endregion

        #region TerrainHeight

        int worldHeight;
        int biomeHeight;
        int riverHeight = 0;

        if (cachedWorldHeight == 0)
        {
            worldHeight = GetWorldHeight(world, pos);

            SubBiome[] adjacentBiomes = GetAdjacentBiomes(world, cc);

            if (isRiver)
            {
                riverHeight = RiverWaterfallBlend(world, cc, pos, adjacentBiomes);
                biomeHeight = riverHeight - worldHeight + chunkBiome.height;
            }

            else
            {
                biomeHeight = GetBiomeHeight(world, pos, chunkBiome);
            }

            //  Blend

            if (IsBlend(isBlend, adjacentBiomes))
            {
                //  Starts blending chunks sides where the adjacent biome is not same as chunk biome
                //  Does this by multiplying biomeheight with a value approaching 0, depending on distance
                //  So on the chunk border, only the world height applies, giving smooth blend

                biomeHeight = Mathf.FloorToInt((float)biomeHeight * TerrainBlend(world, adjacentBiomes, cc, pos));

                chunkBiome = BiomeBlend(world, new Vector2(pos.x, pos.z), adjacentBiomes);
            }

            //  Cache

            world.chunksInWorld[cc.x, cc.z].cachedWorldHeight = worldHeight;
            world.chunksInWorld[cc.x, cc.z].cachedBiomeHeight = biomeHeight;
            world.chunksInWorld[cc.x, cc.z].cachedBlendBiome = chunkBiome;
        }

        else
        {
            worldHeight = cachedWorldHeight;
            biomeHeight = cachedBiomeHeight;
            chunkBiome = cachedBlendBiome;

            if(isRiver)
            {
                riverHeight = GetRiverHeight(world, cc);
            } 
        }

        int terrainHeight = worldHeight + biomeHeight;

        int yPos = Mathf.FloorToInt(pos.y);

        if (1f < yPos && yPos <= terrainHeight)
        {
            //  We dont check if we are lowest voxel or we are in the sky

            isCave = IsCave(world, terrainHeight, pos, yPos);
        }

        #endregion

        #region Structures

        if(yPos == terrainHeight)
        {
            foreach (Structure structure in chunkBiome.structures)
            {
                if (Noise.Get2DPerlinStructureThreshold(pos2d, structure.offset) >= structure.threshold)
                {
                    Queue<VoxelModification> queue = new Queue<VoxelModification>();

                    foreach (VoxelModification vm in structure.voxels)
                    {
                        queue.Enqueue(new VoxelModification(vm.position + pos, vm.id));
                    }

                    world.modifications.Enqueue(queue);

                    return structure.voxels[0].id;
                }
            }
        }


        #endregion

        #region TreeStructure

        if (yPos == terrainHeight)
        {
            foreach (TreeStructure treeStructure in chunkBiome.treeStructures)
            {
                if (Noise.Get2DPerlinStructureThreshold(pos2d, treeStructure.offset) >= treeStructure.threshold)
                {
                    Queue<VoxelModification> queue = new Queue<VoxelModification>();

                    foreach (VoxelModification vm in treeStructure.GetVoxels(pos2d))
                    {
                        queue.Enqueue(new VoxelModification(vm.position + pos, vm.id));
                    }

                    world.modifications.Enqueue(queue);
                }
            }
        }

        #endregion

        #region BlockType

        //  Determines the blocktype according to parameters

        if (isCave)
        {
            return 0;
        }

        else if (yPos == terrainHeight) return chunkBiome.surfaceBlockId;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4) return chunkBiome.subSurfaceBlockId;
        else if (yPos < terrainHeight) return 3;
        else if (yPos < VoxelData.waterHeight) return 8;

        else if(isRiver && yPos < riverHeight - world.riverReference.height)
        {
            return 8;
        }

        return 0;

        #endregion
    }

    private static int GetWorldHeight(World world, Vector3 pos)
    {
        //  The global noise

        return Noise.Get2DPerlinWorld(world, new Vector2(pos.x, pos.z));
    }

    private static int GetBiomeHeight(World world, Vector3 pos, SubBiome chunkBiome)
    {
        //  The biome noise

        return Mathf.FloorToInt(chunkBiome.height * Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, chunkBiome.noiseScale, chunkBiome.octaves, chunkBiome.persistence, chunkBiome.lacunarity, chunkBiome.noiseCutoff));
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
                terrainHeight = Mathf.Min(terrainHeight, GetWorldHeight(world, new Vector3((xPos * chunkWidth) + x, 0, (zPos * chunkWidth) + z)));
            }
        }

        if(chunk != null)
        {
            chunk.riverWaterHeight = terrainHeight;
        }

        return terrainHeight;
    }

    public static bool IsCave(World world, int terrainHeight, Vector3 position, int yPos)
    {
        //  A lot of perlin noise to check if voxel is cave

        return Noise.Get3DPerlinCave(world, terrainHeight, position, yPos);
    }


    private static bool IsBlend(bool isBlend, SubBiome[] adjacentBiomes)
    {
        //  We check if there are any adjacent biomes not equal to chunkbiome,
        //  if there is only one unique biome in list, all 9 are the same, and we dont blend

        if (isBlend && adjacentBiomes.Distinct().ToArray().Length > 1)
        {
            return true;
        }

        return false;
    }

    private static float TerrainBlend(World world, SubBiome[] adjacentBiomes, ChunkCoordinate cc, Vector3 pos)
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

        float noise = Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, 10, 4, 0.5f, 2f, 1);
        float noiseImpact = adjacentBiomes[4].blendNoiseImpact;
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

        if(blendValue >= blendDistance)
        {
            return 1;
        }

        else
        {
            blendValue /= blendDistance;

            blendValue = (blendValue * (1 - noiseImpact)) + (noise * noiseImpact);

            return blendValue;
        }
    }

    private static SubBiome BiomeBlend(World world, Vector2 pos, SubBiome[] adjacentBiomes)
    {
        //  Gets some noise and uses that noise to determine which adjacent chunk we use as blend (very ugly implemnetation pls fix)

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

    private static int RiverWaterfallBlend(World world, ChunkCoordinate cc, Vector3 pos, SubBiome[] adjacentBiomes)
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

    public static Tuple<int, int> GetBorderVoxels(int i)
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

    public static Tuple<int, int> GetAdjacentRiverPos(int i)
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

    private static Tuple<int, int> GetPositionInChunk(ChunkCoordinate cc, Vector3 pos)
    {
        //  Takes worldposition of voxel minus world postion of chunk
        //  Voxels position is always local position larger, so we end up with local postion

        int x = Mathf.FloorToInt(pos.x) - (cc.x * VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z) - (cc.z * VoxelData.chunkWidth);

        return Tuple.Create(x + 1, z + 1);
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

    private static bool IsVoxelInWorld(Vector3 pos)
    {
        //  Checks if voxel is inside world

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxel && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxel)
            return true;
        else
            return false;
    }
}