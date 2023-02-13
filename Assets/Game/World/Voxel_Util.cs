using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public static class Voxel_Util
{
    public static ChunkCoordinate GetChunkCoordinateFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return new ChunkCoordinate(x, z);
    }

    public static bool IsChunkInWorld(ChunkCoordinate coord)
    {
        //  Checks if chunk is inside world

        if (coord.x >= 0 && coord.x < VoxelData.worldSizeInChunks && coord.z >= 0 && coord.z < VoxelData.worldSizeInChunks)
            return true;
        else
            return false;
    }

    public static Chunk GetChunkFromVector3(World world, Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return world.chunksInWorld[x, z];
    }

    public static bool CheckIfVoxelTransparent(World world, Vector3 pos, int cachedWorldHeight, int cachedBiomeHeight, int cachedSubBiomeHeight, SubBiome cachedBlendBiome)
    {
        //  First looks if the voxel is in world, if not in world, return true,
        //  (face generation scrip doesnt recognize adjacent chunks as in world sometimes and we get into first check when we shoudlnt, so we draw face just in case)
        //  Then check chunk who is in world to check if chunk has been loaded, if it has, use istransparent
        //  If it hasnt been loaded, use get voxel

        ChunkCoordinate thisChunk = new ChunkCoordinate(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y >= VoxelData.chunkHeight)
        {
            return true;
        }

        if (world.chunksInWorld[thisChunk.x, thisChunk.z] != null && world.chunksInWorld[thisChunk.x, thisChunk.z].IsEditable)
        {
            return world.blocks[world.chunksInWorld[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;
        }

        return world.blocks[WorldTerrain.GetVoxelBlockType(world, pos, true, cachedWorldHeight, cachedBiomeHeight, cachedSubBiomeHeight, cachedBlendBiome)].isTransparent;
    }

    public static bool CheckIfVoxelLiquid(World world, Vector3 pos, int cachedWorldHeight, int cachedBiomeHeight, int cachedSubBiomeHeight, SubBiome cachedBlendBiome)
    {
        //  First looks if the voxel is in world, if not in world, return true,
        //  (face generation scrip doesnt recognize adjacent chunks as in world sometimes and we get into first check when we shoudlnt, so we draw face just in case)
        //  Then check chunk who is in world to check if chunk has been loaded, if it has, use isLiquid
        //  If it hasnt been loaded, use get voxel

        ChunkCoordinate thisChunk = new ChunkCoordinate(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y >= VoxelData.chunkHeight)
        {
            return false;
        }

        if (world.chunksInWorld[thisChunk.x, thisChunk.z] != null && world.chunksInWorld[thisChunk.x, thisChunk.z].IsEditable)
        {
            return world.blocks[world.chunksInWorld[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isLiquid;
        }

        return world.blocks[WorldTerrain.GetVoxelBlockType(world, pos, true, cachedWorldHeight, cachedBiomeHeight, cachedSubBiomeHeight, cachedBlendBiome)].isLiquid;
    }
}