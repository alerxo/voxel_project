using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownTest : MonoBehaviour
{
    public SubBiome voidBiome;
    public Town testTown;
    public int chunkAmount;

    private int yPos = 100;

    private void Start()
    {
        Debug.Log("Generating world");

        World world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();

        int start = (VoxelData.worldSizeInChunks / 2) - chunkAmount;
        int end = (VoxelData.worldSizeInChunks / 2) + chunkAmount;

        for (int x = start; x < end; x++)
        {
            for (int z = start; z < end; z++)
            {
                world.EnqueueNewChunk(x, z);
                world.chunksInWorld[x, z].IsActive = true;
            }
        }

        Debug.Log("Generating town");

        Vector3 pos = world.chunksInWorld[VoxelData.worldSizeInChunks / 2, VoxelData.worldSizeInChunks / 2].position;
        pos = new Vector3(pos.x, yPos, pos.z);

        TownData townData = new TownData(testTown, pos);

        townData = GenerateTown.GetTownData(townData);

        List<Tuple<Structure, Vector2>> structureData = townData.structures;
        Queue<VoxelModification> roadData = townData.roadData;

        foreach(Tuple<Structure, Vector2> s in structureData)
        {
            Queue<VoxelModification> queue = new Queue<VoxelModification>();

            Structure structure = s.Item1;
            Vector3 structurePosition = new Vector3(s.Item2.x, yPos, s.Item2.y);

            foreach (VoxelModification vm in structure.GetVoxels())
            {
                queue.Enqueue(new VoxelModification(vm.position + structurePosition, vm.blockId));
            }

            world.modifications.Enqueue(queue);
        }

        Queue<VoxelModification> updatedRoadData = new Queue<VoxelModification>();

        foreach (VoxelModification vm in roadData)
        {
            updatedRoadData.Enqueue(new VoxelModification(vm.position + pos, vm.blockId));
        }

        world.modifications.Enqueue(updatedRoadData);

        Debug.Log("Done");
    }
}
