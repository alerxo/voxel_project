using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Connection
{
    public Town_Node startNode;
    public Town_Node endNode;

    public bool isBad = false;

    public Town_Connection(Town_Node a, Town_Node b)
    {
        startNode = a;
        endNode = b;
    }

    public bool IsEqualTo(Town_Connection connection)
    {
        if ((startNode == connection.startNode && endNode == connection.endNode) || (startNode == connection.endNode && endNode == connection.startNode))
        {
            return true;
        }

        return false;
    }

    public void SetBad()
    {
        isBad = true;

        RemoveConnection();
    }

    public void RemoveConnection()
    {
        startNode.RemoveConnection(endNode);
        endNode.RemoveConnection(startNode);
    }
}
