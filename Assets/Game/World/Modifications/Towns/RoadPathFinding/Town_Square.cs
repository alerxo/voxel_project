using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Square
{
    public bool isBad;
    public bool isRoad;
    public Vector2 position;

    public int gCost;
    public int hCost;

    public Town_Square parent;

    public Town_Square(bool _isBad, Vector2 _position)
    {
        isBad = _isBad;
        position = _position;
    }

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public int GetDistanceToOtherSquare(Town_Square otherSquare)
    {
        float xDistance = Mathf.Abs(position.x - otherSquare.position.x);
        float yDistance = Mathf.Abs(position.y - otherSquare.position.y);

        if(xDistance > yDistance)
        {
            return Mathf.FloorToInt(14 * yDistance + 10 * (xDistance - yDistance));
        }

        return Mathf.FloorToInt(14 * xDistance + 10 * (yDistance - xDistance));

    }
}
