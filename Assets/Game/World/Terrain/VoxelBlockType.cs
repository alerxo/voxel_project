using System.Collections.Generic;
using UnityEngine;

public static class VoxelBlockType
{
    public static byte GetVoxelBlockType(World world, Vector3 position, int terrainHeight, SubBiome blendBiome)
    {
        ChunkCoordinate cc = Voxel_Util.GetChunkCoordinateFromVector3(position);
        SubBiome subBiome = world.subBiomeMap[cc.x + 1, cc.z + 1];
        int yPos = Mathf.FloorToInt(position.y);

        if (!IsVoxelInWorld(position) || TestScene(subBiome))
        {
            //  If outside world, return air
            return 0;
        }

        Vector2 position2 = new Vector2(position.x, position.z);

        if (blendBiome != null)
        {
            subBiome = blendBiome;
        }

        if (yPos == terrainHeight)
        {
            CheckStructures(world, position, position2, subBiome);
            CheckVegetation(world, position, position2, subBiome);
        }

        //  Determines the blocktype according to parameters

        if (subBiome == world.riverReference)
        {
            int riverHeight = TerrainHeight.GetRiverHeight(world, cc);

            if (yPos < riverHeight - world.riverReference.height)
            {
                return 11;
            }
        }

        else if (yPos == terrainHeight) return subBiome.surfaceBlockId;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4) return subBiome.subSurfaceBlockId;
        else if (yPos < terrainHeight) return 4;
        else if (yPos < World_Settings.waterHeight) return 11;

        return 0;
    }

    private static void CheckStructures(World world, Vector3 position, Vector2 position2, SubBiome subBiome)
    {
        foreach (Structure structure in subBiome.structures)
        {
            if (Noise.Get2DPerlinStructureThreshold(position2, structure.offset) >= structure.threshold)
            {
                Queue<VoxelModification> queue = new Queue<VoxelModification>();

                foreach (VoxelModification vm in structure.GetVoxels())
                {
                    queue.Enqueue(new VoxelModification(vm.position + position, vm.blockId));
                }

                world.EnqueueModification(queue);
            }
        }
    }

    private static void CheckVegetation(World world, Vector3 position, Vector2 position2, SubBiome subBiome)
    {
        foreach (Vegetation treeStructure in subBiome.treeStructures)
        {
            if (Noise.Get2DPerlinStructureThreshold(position2, treeStructure.offset) >= treeStructure.threshold)
            {
                Queue<VoxelModification> queue = new Queue<VoxelModification>();

                foreach (VoxelModification vm in treeStructure.GetVoxels(position2))
                {
                    queue.Enqueue(new VoxelModification(vm.position + position, vm.blockId));
                }

                world.EnqueueModification(queue);
            }
        }
    }

    private static bool TestScene(SubBiome subBiome)
    {
        if (subBiome != null && subBiome.subBiomeName == "Void")
        {
            return true;
        }

        return false;
    }

    public static SubBiome BiomeBlend(World world, Vector2 pos, SubBiome[] adjacentBiomes)
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

    private static bool IsVoxelInWorld(Vector3 pos)
    {
        //  Checks if voxel is inside world

        if (pos.x >= 0 && pos.x < World_Settings.WorldSizeInVoxel && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < World_Settings.WorldSizeInVoxel)
            return true;
        else
            return false;
    }
}
