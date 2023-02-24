using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Town_Triangle
{
    public Town_Node nodeA;
    public Town_Node nodeB;
    public Town_Node nodeC;

    public bool isBad = false;

    public Town_Triangle(Town_Node a, Town_Node b, Town_Node c)
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

    public bool ContainsNode(Town_Node node)
    {
        if(nodeA == node || nodeB == node || nodeC == node)
        {
            return true;
        }

        return false;
    }

    public void SetBad()
    {
        isBad = true;
    }

    public bool IsNodeInsideCircumcirlce(Vector2 nodePosition)
    {
        Vector2 a = nodeA.position;
        Vector2 b = nodeB.position;
        Vector2 c = nodeC.position;

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
