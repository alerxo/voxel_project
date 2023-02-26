using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class StructureEditor : MonoBehaviour
{
    public SubBiome voidBiome;
    public Structure testStructure;
    public int chunkAmount;

    private int yPos = 100;

    [SerializeField] private bool raw;

    private void Start()
    {
        Debug.Log("Loading");

        World world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();

        int start = (World_Settings.worldSizeInChunks / 2) - chunkAmount;
        int end = (World_Settings.worldSizeInChunks / 2) + chunkAmount;

        for (int x = start; x < end; x++)
        {
            for (int z = start; z < end; z++)
            {
                world.EnqueueNewChunk(x, z);
                world.chunksInWorld[x, z].IsActive = true;
            }
        }

        Vector3 pos = world.chunksInWorld[World_Settings.worldSizeInChunks / 2, World_Settings.worldSizeInChunks / 2].position;
        pos = new Vector3(pos.x, yPos, pos.z);

        Queue<VoxelModification> queue = new Queue<VoxelModification>();

        int count = 0;

        if (raw)
        {
            foreach (VoxelModification vm in testStructure.GetVoxelsRaw())
            {
                queue.Enqueue(new VoxelModification(vm.position + pos, vm.blockId));
                count++;
            }
        }

        else
        {
            foreach (VoxelModification vm in testStructure.GetVoxels())
            {
                queue.Enqueue(new VoxelModification(vm.position + pos, vm.blockId));
                count++;
            }
        }

        Debug.Log(count + " voxels loaded");

        world.modifications.Enqueue(queue);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q) && raw)
        {
            Debug.Log("Saving");

            List<VoxelModification> vm = new List<VoxelModification>();

            Vector3 northEastUpperPoint = new Vector3(1, 1, 1);
            Vector3 southWestLowerPoint = new Vector3(-1, -1, -1);

            World world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();

            int start = (World_Settings.worldSizeInChunks / 2) - chunkAmount;
            int end = (World_Settings.worldSizeInChunks / 2) + chunkAmount;

            int chunkWidth = VoxelData.chunkWidth;
            int chunkHeight = VoxelData.chunkHeight;

            Vector3 pos = world.chunksInWorld[World_Settings.worldSizeInChunks / 2, World_Settings.worldSizeInChunks / 2].position;
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
                                    Vector3 positio = new Vector3((x * chunkWidth) + cx, cy, (z * chunkWidth) + cz);
                                    positio -= pos;

                                    #region zone

                                    if (positio.x > northEastUpperPoint.x)
                                    {
                                        northEastUpperPoint.x = positio.x;
                                    }

                                    if (positio.y > northEastUpperPoint.y)
                                    {
                                        northEastUpperPoint.y = positio.y;
                                    }

                                    if (positio.z > northEastUpperPoint.z)
                                    {
                                        northEastUpperPoint.z = positio.z;
                                    }

                                    if (positio.x < southWestLowerPoint.x)
                                    {
                                        southWestLowerPoint.x = positio.x;
                                    }

                                    if (positio.y < southWestLowerPoint.y)
                                    {
                                        southWestLowerPoint.y = positio.y;
                                    }

                                    if (positio.z < southWestLowerPoint.z)
                                    {
                                        southWestLowerPoint.z = positio.z;
                                    }

                                    #endregion

                                    Vector3 position = new Vector3((x * chunkWidth) + cx, cy, (z * chunkWidth) + cz);
                                    vm.Add(new VoxelModification(position - pos, id));
                                }
                            }
                        }
                    }
                }
            }

            float[] NEU = new float[3]
            {
                northEastUpperPoint.x,
                northEastUpperPoint.y,
                northEastUpperPoint.z
            };

            float[] SWL = new float[3]
            {
                southWestLowerPoint.x,
                southWestLowerPoint.y,
                southWestLowerPoint.z
            };

            VoxelModification[] voxels = vm.ToArray();

            int count = 0;

            foreach(VoxelModification vmssd in voxels)
            {
                count++;
            }

            Debug.Log(count + " voxels saved");

            SaveStructureData.SaveData(testStructure, voxels, NEU, SWL);

            Structure_Util.DrawZone(testStructure, pos);
        }

        else if(Input.GetKeyDown(KeyCode.Q) && !raw)
        {
            Debug.Log("Cannot save as structure not loaded raw");
        }
    }
}
