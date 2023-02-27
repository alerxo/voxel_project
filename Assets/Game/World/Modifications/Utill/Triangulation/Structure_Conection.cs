using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure_Connection
{
    public Structure_Node startNode;
    public Structure_Node endNode;

    public bool isBad = false;

    public Structure_Connection(Structure_Node a, Structure_Node b)
    {
        startNode = a;
        endNode = b;
    }

    public bool IsEqualTo(Structure_Connection connection)
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
