using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StructureData
{
    public Tuple<byte, float[]>[] voxels;

    public float[] northEastUpperPoint;
    public float[] southWestLowerPoint;

    public StructureData()
    {
        float[] pos = new float[3];

        pos[0] = 0f;
        pos[1] = 0f;
        pos[2] = 0f;

        byte id = 4;

        Tuple<byte, float[]> voxel = Tuple.Create(id, pos);

        voxels = new Tuple<byte, float[]>[1];

        voxels[0] = voxel;

        northEastUpperPoint = new float[3]
        {
            1,
            1,
            1
        };

        southWestLowerPoint = new float[3] 
        {
            -1,
            -1,
            -1
        };
    }

    public StructureData(VoxelModification[] voxelModifications, float[] NEU, float[] SWL)
    {
        List<Tuple<byte, float[]>> voxelList = new List<Tuple<byte, float[]>>();

        foreach(VoxelModification voxel in voxelModifications)
        {
            float[] pos = new float[3];

            pos[0] = voxel.position.x;
            pos[1] = voxel.position.y;
            pos[2] = voxel.position.z;

            voxelList.Add(Tuple.Create(voxel.blockId, pos));
        }

        voxels = voxelList.ToArray();

        northEastUpperPoint = NEU;
        southWestLowerPoint = SWL;
    }
}