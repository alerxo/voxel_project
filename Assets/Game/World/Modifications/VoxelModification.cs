using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxelModification
{
    //  A class storing position and block id for chunk to read and apply

    public Vector3 position;
    public byte id;

    public VoxelModification()
    {
        position = new Vector3();
        id = 0;
    }

    public VoxelModification (Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
}
