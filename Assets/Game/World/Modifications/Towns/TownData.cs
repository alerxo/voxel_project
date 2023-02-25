using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownData
{
    public Town town;

    public Vector3 position;
    public Vector2 position2;

    public int sizeRadius;
    public float density;

    public List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();
    public Queue<VoxelModification> roadData = new Queue<VoxelModification>();

    public TownData(Town _town, Vector3 _position)
    {
        town = _town;

        position = _position;
        position2 = new Vector2(position.x, position.z);
    }
}
