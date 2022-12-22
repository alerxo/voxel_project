using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WorldTerrain
{
    public static byte GetVoxelBlockType(World world, Vector3 pos)
    {
        //  Goes through different blocks of code to determine terrainheight and blocktypes

        //  Gets the biome this position is in

        Biome biome = GetBiome(world, pos);

        int terrainheight;
        int yPos = Mathf.FloorToInt(pos.y);

        //  Constant, these are always true

        if (!IsVoxelInWorld(pos))
        {
            //  If outside world, return air
            return 0;
        }

        //  Terrain, gets the terrainheigt through layered perlin noise

        terrainheight = Mathf.FloorToInt(biome.height * Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, biome.noiseScale, biome.octaves, biome.persistence, biome.lacunarity)) + biome.startHeight;

        //  Falloff, terrain thats 0.5 worldsize from spawn, starts to get multiplied by a value aproaching 0,
        //  meaning the terrain falls off to be surrounded by water

        if(Noise.GetFallOffMap(new Vector2(pos.x, pos.z)) < 0.5f)
        {
            terrainheight = Mathf.FloorToInt(2 * terrainheight * Noise.GetFallOffMap(new Vector2(pos.x, pos.z)));
        }

        //  Block, determines the blocktype

        if (yPos == terrainheight) return biome.surfaceBlockId;
        else if (yPos < terrainheight && yPos > terrainheight - 4) return biome.subSurfaceBlockId;
        else if (yPos < terrainheight) return 3;
        else if (yPos < 64) return 9;
        else return 0;
    }

    private static Biome GetBiome(World world, Vector3 pos)
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

    private static bool IsVoxelInWorld(Vector3 pos)
    {
        //  Checks if voxel is inside world

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxel && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxel)
            return true;
        else
            return false;
    }
}
