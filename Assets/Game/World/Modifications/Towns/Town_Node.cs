using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Node
{
    public List<Town_Node> connectedNodes = new List<Town_Node>();
    public List<Town_Node> finalConnectedNodes = new List<Town_Node>();

    public Vector2 position;



    public bool isBad { get; private set; }

    public Town_Node(Vector2 pos)
    {
        isBad = false;
        position = pos;
    }

    public void AddConnection(Town_Node connectedNode)
    {
        if (!connectedNodes.Contains(connectedNode))
        {
            connectedNodes.Add(connectedNode);
        }
    }

    public void RemoveConnection(Town_Node connectedNode)
    {
        if (connectedNodes.Contains(connectedNode))
        {
            connectedNodes.Remove(connectedNode);
        }
    }

    public void SetBad()
    {
        isBad = true;
    }

    public static void ConnectNodes(Town_Node nodeA, Town_Node nodeB, Town_Node nodeC)
    {
        if (nodeA.isBad || nodeB.isBad || nodeC.isBad) return;

        nodeA.AddConnection(nodeB);
        nodeA.AddConnection(nodeC);

        nodeB.AddConnection(nodeA);
        nodeB.AddConnection(nodeC);

        nodeC.AddConnection(nodeA);
        nodeC.AddConnection(nodeB);
    }

    public Town_Node GetShortestConnection()
    {
        //if(shortestConnection != null && !shortestConnection.isBad)
        //{
        //    return shortestConnection;
        //}

        Town_Node closestNode = null;
        float distanceToClosestNode = float.MaxValue;

        foreach(Town_Node connection in connectedNodes)
        {
            float tempDistance = Vector2.Distance(position, connection.position);

            if (tempDistance < distanceToClosestNode)
            {
                closestNode = connection;
                distanceToClosestNode = tempDistance;
            }
        }

        return closestNode;
    }
}
