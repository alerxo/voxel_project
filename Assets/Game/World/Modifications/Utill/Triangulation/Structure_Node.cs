using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure_Node
{
    public List<Structure_Node> connectedNodes = new List<Structure_Node>();
    public List<Structure_Node> finalConnectedNodes = new List<Structure_Node>();

    public Vector2 position;

    public bool isBad { get; private set; }

    public Structure_Node(Vector2 pos)
    {
        isBad = false;
        position = pos;
    }

    public void AddConnection(Structure_Node connectedNode)
    {
        if (!connectedNodes.Contains(connectedNode) && !connectedNode.isBad)
        {
            connectedNodes.Add(connectedNode);
        }
    }

    public void RemoveConnection(Structure_Node connectedNode)
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

    public static void ConnectNodes(Structure_Node nodeA, Structure_Node nodeB, Structure_Node nodeC)
    {
        nodeA.AddConnection(nodeB);
        nodeA.AddConnection(nodeC);

        nodeB.AddConnection(nodeA);
        nodeB.AddConnection(nodeC);

        nodeC.AddConnection(nodeA);
        nodeC.AddConnection(nodeB);
    }
}
