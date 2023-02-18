using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Triangle
{
    public Vector2 pointA;
    public Vector2 pointB;
    public Vector2 pointC;

    public bool isBad = false;

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

    public bool IsPointInsideCircumcirlce(Vector2 point)
    {
        Vector2 a = pointA;
        Vector2 b = pointB;
        Vector2 c = pointC;

        float d = ((a.x * (b.y - c.y)) + (b.x * (c.y - a.y)) + (c.x * (a.y - b.y))) * 2f;

        float x = (1f / d) * (((Mathf.Pow(a.x, 2) + Mathf.Pow(a.y, 2)) * (b.y - c.y)) + ((Mathf.Pow(b.x, 2) + Mathf.Pow(b.y, 2)) * (c.y - a.y)) + ((Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2)) * (a.y - b.y)));
        float y = (1f / d) * (((Mathf.Pow(a.x, 2) + Mathf.Pow(a.y, 2)) * (c.x - b.x)) + ((Mathf.Pow(b.x, 2) + Mathf.Pow(b.y, 2)) * (a.x - c.x)) + ((Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2)) * (b.x - a.x)));

        Vector2 circumcentre = new Vector2(x, y);

        float radius = Vector2.Distance(b, circumcentre);

        if (Vector2.Distance(point, circumcentre) <= radius)
        {
            return true;
        }

        return false;
    }
}
