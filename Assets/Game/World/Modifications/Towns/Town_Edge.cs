using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Edge
{
    public Vector2 pointA;
    public Vector2 pointB;

    public Town_Edge(Vector2 a, Vector2 b)
    {
        pointA = a;
        pointB = b;
    }

    public bool EdgeIsSharedWithAnyIn(List<Town_Edge> edges)
    {
        foreach (Town_Edge edge in edges)
        {
            if (edge != this && Vector2.Distance(edge.pointA, pointA) < 0.1f && Vector2.Distance(edge.pointB, pointB) < 0.1f)
            {
                return true;
            }
        }

        return false;
    }
}
