using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Structure_Triangle
{
    public Structure_Node nodeA;
    public Structure_Node nodeB;
    public Structure_Node nodeC;

    public bool isBad = false;

    public Structure_Triangle(Structure_Node a, Structure_Node b, Structure_Node c)
    {
        nodeA = a;
        nodeB = b;
        nodeC = c;
    }

    public Structure_Connection[] GetConnections()
    {
        return new Structure_Connection[3] 
        {
            new Structure_Connection(nodeA, nodeB),
            new Structure_Connection(nodeB, nodeC),
            new Structure_Connection(nodeC, nodeA)
        };
    }

    public bool ContainsNode(Structure_Node node)
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
