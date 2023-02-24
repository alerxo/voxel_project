using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Square
{
    public bool isBad;
    public bool isRoad;
    public Vector2 position;

    public Town_Square(bool _isBad, Vector2 _position)
    {
        isBad = _isBad;
        position = _position;
    }
}
