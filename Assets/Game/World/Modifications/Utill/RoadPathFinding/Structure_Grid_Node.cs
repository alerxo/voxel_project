using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure_Grid_Node
{
    public bool isBad;
    public bool isPath;
    public Vector2 position;

    public int gCost;
    public int hCost;

    public Structure_Grid_Node parent;

    public Structure_Grid_Node(bool _isBad, Vector2 _position)
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

    public int GetDistanceToOtherNode(Structure_Grid_Node otherNode)
    {
        float xDistance = Mathf.Abs(position.x - otherNode.position.x);
        float yDistance = Mathf.Abs(position.y - otherNode.position.y);

        if(xDistance > yDistance)
        {
            return Mathf.FloorToInt(14 * yDistance + 10 * (xDistance - yDistance));
        }

        return Mathf.FloorToInt(14 * xDistance + 10 * (yDistance - xDistance));

    }
}
