using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StructureData
{
    public Tuple<byte, float[]>[] voxels;

    public StructureData()
    {
        float[] pos = new float[3];

        pos[0] = 0f;
        pos[1] = 0f;
        pos[2] = 0f;

        byte id = 3;

        Tuple<byte, float[]> voxel = Tuple.Create(id, pos);

        voxels = new Tuple<byte, float[]>[1];

        voxels[0] = voxel;
    }

    public StructureData(VoxelModification[] voxelModifications)
    {
        List<Tuple<byte, float[]>> voxelList = new List<Tuple<byte, float[]>>();

        foreach(VoxelModification voxel in voxelModifications)
        {
            float[] pos = new float[3];

            pos[0] = voxel.position.x;
            pos[1] = voxel.position.y;
            pos[2] = voxel.position.z;

            voxelList.Add(Tuple.Create(voxel.id, pos));
        }

        voxels = voxelList.ToArray();   
    }
}