using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float Get2DPerlin(World world, Vector2 pos, float offset, float scale)
    {
        //  Returns perlin

        return Mathf.PerlinNoise((pos.x + 0.1f) / VoxelData.chunkWidth * scale + offset + world.randomSeed, (pos.y + 0.1f) / VoxelData.chunkWidth * scale + offset + world.randomSeed);
    }

    public static float GetFallOffMap(Vector2 pos)
    {
        float halfWorldSizeInVoxel = VoxelData.WorldSizeInVoxel / 2;

        float x = Mathf.Abs(Mathf.FloorToInt(pos.x) - halfWorldSizeInVoxel);
        float y = Mathf.Abs(Mathf.FloorToInt(pos.y) - halfWorldSizeInVoxel);

        if (x > y) return 1 - (x / halfWorldSizeInVoxel);
        else return 1 - (y / halfWorldSizeInVoxel);
    }

    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold)
    {
        //  Returns 3d noise base of mathimatichal mathimeaticds

        float x = (position.x + offset + 0.1f) * scale;
        float y = (position.y + offset + 0.1f) * scale;
        float z = (position.z + offset + 0.1f) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        if((AB + BC + AC + CA + BA + CB + CA) / 6f > threshold)
            return true;
        else
            return false;
    }
}
