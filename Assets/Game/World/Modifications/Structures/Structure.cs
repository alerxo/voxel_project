using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Structure", menuName = "Modifications/Structure")]
public class Structure : ScriptableObject
{
    public string structureName;

    public float threshold;
    public float offset;

    public int zoneMargin;

    public float buildRadius;

    public VoxelModification[] GetVoxels()
    {
        //  Loads the structure data from the binary file loader and turns it into an array of voxelmodifications

        StructureData structureData = SaveStructureData.LoadData(this);

        List<VoxelModification> voxelList = new List<VoxelModification>();

        foreach (Tuple<byte, float[]> voxelData in structureData.voxels)
        {
            byte id = voxelData.Item1;

            if (id == 1) id = 0; // Transform solid air blocks into normal air blocks

            float[] posArray = voxelData.Item2;

            Vector3 position = new Vector3(posArray[0], posArray[1], posArray[2]);

            VoxelModification vm = new VoxelModification(position, id);

            voxelList.Add(vm);
        }

        return voxelList.ToArray();
    }

    public VoxelModification[] GetVoxelsRaw()
    {
        //  Loads the structure data from the binary file loader and turns it into an array of voxelmodifications
        //  Raw version meaning we want solid airblocks to stay solid

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

    public Vector3[] GetZone()
    {
        //  Gets two corners of the structure creating a rectangle around the structure

        StructureData structureData = SaveStructureData.LoadData(this);

        Vector3[] zonePoints = new Vector3[2]
        {
            new Vector3(structureData.northEastUpperPoint[0], structureData.northEastUpperPoint[1], structureData.northEastUpperPoint[2]),
            new Vector3(structureData.southWestLowerPoint[0], structureData.southWestLowerPoint[1], structureData.southWestLowerPoint[2])
        };

        return zonePoints;
    }
}
