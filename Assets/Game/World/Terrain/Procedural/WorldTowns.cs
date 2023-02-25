using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldTowns
{
    public static void GenerateTowns(World world, Town town)
    {
        GetTown(world, town);
    }

    public static void GetTown(World world, Town town)
    {
        Vector3 pos = world.chunksInWorld[VoxelData.worldSizeInChunks / 2, VoxelData.worldSizeInChunks / 2].position;

        TownData townData = new TownData(town, pos);

        townData = GenerateTown.GetTownData(townData);

        List<Tuple<Structure, Vector2>> structureData = townData.structures;
        Queue<VoxelModification> roadData = townData.roadData;

        foreach (Tuple<Structure, Vector2> s in structureData)
        {
            Queue<VoxelModification> queue = new Queue<VoxelModification>();

            Structure structure = s.Item1;
            Vector3 structurePosition = new Vector3(s.Item2.x, 1, s.Item2.y);

            SubBiome structureChunkBiome = Voxel_Util.GetChunkFromVector3(world, structurePosition).subBiome;

            structurePosition.y =  WorldTerrain.GetWorldHeight(world, structurePosition) + WorldTerrain.GetBiomeHeight(world, structurePosition, structureChunkBiome.parentBiome);
            

            foreach (VoxelModification vm in structure.GetVoxels())
            {
                queue.Enqueue(new VoxelModification(vm.position + structurePosition, vm.blockId));
            }

            world.modifications.Enqueue(queue);
        }

        Queue<VoxelModification> updatedRoadData = new Queue<VoxelModification>();

        foreach (VoxelModification vm in roadData)
        {
            Vector3 roadPosition = new Vector3(vm.position.x, 1, vm.position.z) + pos;

            SubBiome structureChunkBiome = Voxel_Util.GetChunkFromVector3(world, roadPosition).subBiome;

            roadPosition.y = WorldTerrain.GetWorldHeight(world, roadPosition) + WorldTerrain.GetBiomeHeight(world, roadPosition, structureChunkBiome.parentBiome);

            updatedRoadData.Enqueue(new VoxelModification(roadPosition, vm.blockId));
        }

        world.modifications.Enqueue(updatedRoadData);
    }
}
