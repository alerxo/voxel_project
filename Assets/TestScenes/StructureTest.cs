using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureTest : MonoBehaviour
{
    public SubBiome voidBiome;
    public Structure testStructure;
    public int chunkAmount;

    private void Start()
    {
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

        Vector3 pos = world.chunksInWorld[VoxelData.worldSizeInChunks / 2, VoxelData.worldSizeInChunks / 2].position;
        pos = new Vector3(pos.x, 100f, pos.z);

        Queue<VoxelModification> queue = new Queue<VoxelModification>();

        foreach (VoxelModification vm in testStructure.voxels)
        {
            queue.Enqueue(new VoxelModification(vm.position + pos, vm.id));
        }

        world.modifications.Enqueue(queue);
    }
}
