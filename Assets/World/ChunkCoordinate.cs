using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkCoordinate
{
    public int x;
    public int z;

    public ChunkCoordinate (int _x, int _z)
    {
        x = _x;
        z = _z;
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
