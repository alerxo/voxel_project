using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class WorldTerrain
{
    public static byte GetVoxelBlockType(World world, Vector3 pos)
    {
        //  World generation basically
        //  Has few parameters per block of code, uses noise to see if it passes parameters, and assigns block thereafter

        Biome biome = GetBiome(world, pos);

        int terrainheight;

        int yPos = Mathf.FloorToInt(pos.y);

        //  Constant

        if (!IsVoxelInWorld(pos))
        {
            //  If outside world, return air
            return 0;
        }

        terrainheight = Mathf.FloorToInt((biome.maxGroundY - biome.minGroundY) * Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, biome.terrainNoiseScale)) + biome.minGroundY;

        if (yPos == terrainheight) return biome.surfaceBlockId;
        else if (yPos < terrainheight && yPos > terrainheight - 4) return biome.subSurfaceBlockId;
        else if (yPos < terrainheight) return 3;
        else if (yPos < 64) return 9;
        else return 0;
    }

    private static Biome GetBiome(World world, Vector3 pos)
    {
        Climate climate = GetClimate(world, pos);

        float noise = Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, 1f);

        if (noise > 0.66f)
            return climate.biomes[0];
        else if (noise > 0.33f)
            return climate.biomes[1];
        else
            return climate.biomes[2];
    }

    private static Climate GetClimate(World world, Vector3 pos)
    {
        float noise = Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, 0.1f);

        if (noise > 0.66f) return world.climates[1];

        else if (noise > 0.33f) return world.climates[0];

        else return world.climates[2];
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
