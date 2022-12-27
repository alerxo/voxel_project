using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkCoordinate
{
    public int x;
    public int z;

    public ChunkCoordinate()
    {
        x = 0;
        z = 0;
    }

    public ChunkCoordinate (int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public ChunkCoordinate(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck / VoxelData.chunkWidth;
        z = zCheck / VoxelData.chunkWidth;

        //  the maths dont work good with negatives

        if (pos.x < 0) x = -1;
        if (pos.z < 0) z = -1;
    }

    public bool Equals (ChunkCoordinate other)
    {
        //  Checks if coordinate equals other

        if(other == null) 
            return false;
        else if(other.x == x && other.z == z)
            return true;
        else
            return false;
    }
}
