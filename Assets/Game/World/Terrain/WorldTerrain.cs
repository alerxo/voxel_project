using System.Collections;
using System.Collections.Generic;
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

        terrainheight = Mathf.FloorToInt((biome.maxTerrainHeight - biome.minTerrainHeight) * Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, biome.noiseScale, biome.octaves, biome.persistence, biome.lacunarity)) + biome.minTerrainHeight;

        //  Falloff, terrain thats 0.5 worldsize from spawn, starts to get multiplies by a value aproaching 0,
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

        if (noise > 0.66f)
            return climate.biomes[0];
        else if (noise > 0.33f)
            return climate.biomes[1];
        else
            return climate.biomes[2];
    }

    private static Climate GetClimate(World world, Vector3 pos)
    {
        //  Value given by perlin noise runs through thresholds

        float noise = Noise.Get2DPerlin(world, new Vector2(pos.x, pos.z), 0, world.noiseScale, world.octaves, world.persistence, world.lacunarity);

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
