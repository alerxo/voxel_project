using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Triangle
{
    public Vector2 pointA;
    public Vector2 pointB;
    public Vector2 pointC;

    public Town_Triangle(Vector2 a, Vector2 b, Vector2 c)
    {
        pointA = a;
        pointB = b;
        pointC = c;
    }

    public Town_Edge[] GetEdges()
    {
        return new Town_Edge[3] 
        {
            new Town_Edge(pointA, pointB),
            new Town_Edge(pointB, pointC),
            new Town_Edge(pointC, pointA)
        };
    }

    public bool ContainsPoint(Vector2 point)
    {
        if(Vector2.Distance(pointA, point) < 0.1f || Vector2.Distance(pointB, point) < 0.1f || Vector2.Distance(pointC, point) < 0.1f)
        {
            return true;
        }

        return false;
    }
}
