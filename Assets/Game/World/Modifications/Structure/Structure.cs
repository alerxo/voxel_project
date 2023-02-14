using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Structure", menuName = "StructureMenu/Structure")]
public class Structure : ScriptableObject
{
    public string structureName;

    public float threshold;
    public float offset;

    public VoxelModification[] GetVoxels()
    {
        StructureData structureData = SaveStructureData.LoadData(this);

        List<VoxelModification> voxelList = new List<VoxelModification>();

        foreach (Tuple<byte, float[]> voxelData in structureData.voxels)
        {
            byte id = voxelData.Item1;
            float[] posArray = voxelData.Item2;

            Vector3 position = new Vector3(posArray[0], posArray[1], posArray[2]);

            VoxelModification vm = new VoxelModification(position, id);

            voxelList.Add(vm);
        }

        return voxelList.ToArray();
    }
}
