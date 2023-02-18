using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Connection
{
    public Vector2 nodeA;
    public Vector2 nodeB;

    public bool isBad = false;

    public Town_Connection(Vector2 a, Vector2 b)
    {
        nodeA = a;
        nodeB = b;
    }

    public static bool ConnectionIsSharedBetween(Town_Connection connectionA, Town_Connection connectionB)
    {
        if ((Vector2.Distance(connectionA.nodeA, connectionB.nodeA) < 1f && Vector2.Distance(connectionA.nodeB, connectionB.nodeB) < 1f) || 
            (Vector2.Distance(connectionA.nodeB, connectionB.nodeA) < 1f && Vector2.Distance(connectionA.nodeA, connectionB.nodeB) < 1f))
        {
            return true;
        }

        return false;
    }
}
