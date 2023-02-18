using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Triangle
{
    public Vector2 nodeA;
    public Vector2 nodeB;
    public Vector2 nodeC;

    public bool isBad = false;

    public Town_Triangle(Vector2 a, Vector2 b, Vector2 c)
    {
        nodeA = a;
        nodeB = b;
        nodeC = c;
    }

    public Town_Connection[] GetConnections()
    {
        return new Town_Connection[3] 
        {
            new Town_Connection(nodeA, nodeB),
            new Town_Connection(nodeB, nodeC),
            new Town_Connection(nodeC, nodeA)
        };
    }

    public bool ContainsNode(Vector2 nodePosition)
    {
        if(Vector2.Distance(nodeA, nodePosition) < 0.1f || Vector2.Distance(nodeB, nodePosition) < 0.1f || Vector2.Distance(nodeC, nodePosition) < 0.1f)
        {
            return true;
        }

        return false;
    }

    public bool IsNodeInsideCircumcirlce(Vector2 nodePosition)
    {
        Vector2 a = nodeA;
        Vector2 b = nodeB;
        Vector2 c = nodeC;

        float d = ((a.x * (b.y - c.y)) + (b.x * (c.y - a.y)) + (c.x * (a.y - b.y))) * 2f;

        float x = (1f / d) * (((Mathf.Pow(a.x, 2) + Mathf.Pow(a.y, 2)) * (b.y - c.y)) + ((Mathf.Pow(b.x, 2) + Mathf.Pow(b.y, 2)) * (c.y - a.y)) + ((Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2)) * (a.y - b.y)));
        float y = (1f / d) * (((Mathf.Pow(a.x, 2) + Mathf.Pow(a.y, 2)) * (c.x - b.x)) + ((Mathf.Pow(b.x, 2) + Mathf.Pow(b.y, 2)) * (a.x - c.x)) + ((Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2)) * (b.x - a.x)));

        Vector2 circumcentre = new Vector2(x, y);

        float radius = Vector2.Distance(b, circumcentre);

        if (Vector2.Distance(nodePosition, circumcentre) <= radius)
        {
            return true;
        }

        return false;
    }
}
