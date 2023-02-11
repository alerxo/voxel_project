using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class StructureTest : MonoBehaviour
{
    public SubBiome voidBiome;
    public Structure testStructure;
    public int chunkAmount;

    private int yPos = 100;

    private void Start()
    {
        Debug.Log("Loading");

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
        pos = new Vector3(pos.x, yPos, pos.z);

        Queue<VoxelModification> queue = new Queue<VoxelModification>();

        foreach (VoxelModification vm in testStructure.GetVoxels())
        {
            queue.Enqueue(new VoxelModification(vm.position + pos, vm.id));
        }

        world.modifications.Enqueue(queue);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Saving");

            List<VoxelModification> vm = new List<VoxelModification>();

            World world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();

            int start = (VoxelData.worldSizeInChunks / 2) - chunkAmount;
            int end = (VoxelData.worldSizeInChunks / 2) + chunkAmount;

            int chunkWidth = VoxelData.chunkWidth;
            int chunkHeight = VoxelData.chunkHeight;

            Vector3 pos = world.chunksInWorld[VoxelData.worldSizeInChunks / 2, VoxelData.worldSizeInChunks / 2].position;
            pos = new Vector3(pos.x, yPos, pos.z);

            for (int x = start; x < end; x++)
            {
                for (int z = start; z < end; z++)
                {
                    Chunk chunk = world.chunksInWorld[x, z];

                    for(int cx = 0; cx < chunkWidth; cx++)
                    {
                        for (int cy = 0; cy < chunkHeight; cy++)
                        {
                            for (int cz = 0; cz < chunkWidth; cz++)
                            {
                                byte id = chunk.voxelMap[cx, cy, cz];

                                if(id != 0)
                                {
                                    Vector3 position = new Vector3((x * chunkWidth) + cx, cy, (z * chunkWidth) + cz);
                                    vm.Add(new VoxelModification(position - pos, id));
                                }
                            }
                        }
                    }
                }
            }

            VoxelModification[] voxels = vm.ToArray();
            SaveStructureData.SaveData(testStructure, voxels);
        }
    }
}
