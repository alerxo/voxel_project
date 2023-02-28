using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure_Util
{
    public static List<Tuple<Structure, Vector3>> structureList = new List<Tuple<Structure, Vector3>>();

    public static void DrawZone(Structure structure, Vector3 positon)
    {
        Vector3[] zonePoints = structure.GetZone();
        Vector3 blockOffset = new Vector3(0.5f, 0.5f, 0.5f) + positon;

        Vector3 xp = new Vector3(zonePoints[0].x + structure.zoneMargin, 0, 0);
        Vector3 yp = new Vector3(0, zonePoints[0].y + structure.zoneMargin, 0);
        Vector3 zp = new Vector3(0, 0, zonePoints[0].z + structure.zoneMargin);
        Vector3 xn = new Vector3(zonePoints[1].x - structure.zoneMargin, 0, 0);
        Vector3 yn = new Vector3(0, zonePoints[1].y - structure.zoneMargin, 0);
        Vector3 zn = new Vector3(0, 0, zonePoints[1].z - structure.zoneMargin);

        Debug.DrawLine(xp + yn + zp + blockOffset, xp + yp + zp + blockOffset, Color.white, 10f);
        Debug.DrawLine(xn + yn + zp + blockOffset, xn + yp + zp + blockOffset, Color.white, 10f);
        Debug.DrawLine(xp + yn + zn + blockOffset, xp + yp + zn + blockOffset, Color.white, 10f);
        Debug.DrawLine(xn + yn + zn + blockOffset, xn + yp + zn + blockOffset, Color.white, 10f);

        Debug.DrawLine(xp + yp + zp + blockOffset, xp + yp + zn + blockOffset, Color.white, 10f);
        Debug.DrawLine(xp + yn + zp + blockOffset, xp + yn + zn + blockOffset, Color.white, 10f);

        Debug.DrawLine(xn + yp + zp + blockOffset, xn + yp + zn + blockOffset, Color.white, 10f);
        Debug.DrawLine(xn + yn + zp + blockOffset, xn + yn + zn + blockOffset, Color.white, 10f);

        Debug.DrawLine(xp + yp + zp + blockOffset, xn + yp + zp + blockOffset, Color.white, 10f);
        Debug.DrawLine(xp + yn + zp + blockOffset, xn + yn + zp + blockOffset, Color.white, 10f);

        Debug.DrawLine(xp + yp + zn + blockOffset, xn + yp + zn + blockOffset, Color.white, 10f);
        Debug.DrawLine(xp + yn + zn + blockOffset, xn + yn + zn + blockOffset, Color.white, 10f);
    }   

    public static bool CheckOverlap(Structure structure, Vector3 position, bool add)
    {
        if(structureList.Count == 0)
        {
            if (add)
            {
                structureList.Add(Tuple.Create(structure, position));
                return false;
            }
        }

        foreach (Tuple<Structure, Vector3> s in structureList)
        {
            if(Vector3.Distance(position, s.Item2) < (structure.buildRadius + s.Item1.buildRadius))
            {
                return true;
            }
        }

        if (add)
        {
            structureList.Add(Tuple.Create(structure, position));
        }

        return false;
    }

    public static bool CheckOverlap(Vector3 position)
    {
        foreach (Tuple<Structure, Vector3> s in structureList)
        {
            if (Vector3.Distance(position, s.Item2) < s.Item1.buildRadius)
            {
                return true;
            }
        }

        return false;
    }

    public static bool CheckOverlap(Vector2 position)
    {
        foreach (Tuple<Structure, Vector3> s in structureList)
        {
            if (Vector2.Distance(position, new Vector2(s.Item2.x, s.Item2.z)) < s.Item1.buildRadius)
            {
                return true;
            }
        }

        return false;
    }


    public static List<Structure_Node> Triangulation(Vector2 position, int size, List<Vector2> nodePoints)
    {
        //  BowyerWatson

        //  Starts by placing the super triangle and sets it bad as we dont want it later
        //  Then starts looping through each node, replacing the triangles when needed

        List<Structure_Triangle> triangles = new List<Structure_Triangle>();

        Structure_Node superPointA = new Structure_Node(position + new Vector2(0, size * 3.2f));
        Structure_Node superPointB = new Structure_Node(position + new Vector2(-size * 2.2f, -size * 1.2f));
        Structure_Node superPointC = new Structure_Node(position + new Vector2(size * 2.2f, -size * 1.2f));

        superPointA.SetBad();
        superPointB.SetBad();
        superPointC.SetBad();

        triangles.Add(new Structure_Triangle(superPointA, superPointB, superPointC));

        foreach (Vector2 nodePoint in nodePoints)
        {
            List<Structure_Connection> connections = new List<Structure_Connection>();

            foreach (Structure_Triangle triangle in triangles)
            {
                if (triangle.IsNodeInsideCircumcirlce(nodePoint))
                {
                    triangle.SetBad();

                    foreach (Structure_Connection connection in triangle.GetConnections())
                    {
                        connections.Add(connection);
                    }
                }
            }

            triangles.RemoveAll((Structure_Triangle t) => t.isBad);

            for (int i = 0; i < connections.Count; i++)
            {
                for (int j = i + 1; j < connections.Count; j++)
                {
                    if (connections[i].IsEqualTo(connections[j]))
                    {
                        connections[i].SetBad();
                        connections[j].SetBad();
                    }
                }
            }

            connections.RemoveAll((Structure_Connection e) => e.isBad);

            Structure_Node newNode = new Structure_Node(nodePoint);

            foreach (Structure_Connection connection in connections)
            {
                triangles.Add(new Structure_Triangle(newNode, connection.startNode, connection.endNode));

                Structure_Node.ConnectNodes(newNode, connection.startNode, connection.endNode);
            }
        }

        triangles.RemoveAll((Structure_Triangle t) => t.ContainsNode(superPointA) || t.ContainsNode(superPointB) || t.ContainsNode(superPointC));

        List<Structure_Connection> finalConnections = new List<Structure_Connection>();
        List<Structure_Node> finalNodes = new List<Structure_Node>();

        foreach (Structure_Triangle triangle in triangles)
        {
            foreach (Structure_Connection connection in triangle.GetConnections())
            {
                finalConnections.Add(connection);

                if (!finalNodes.Contains(connection.startNode))
                {
                    finalNodes.Add(connection.startNode);
                }

                if (!finalNodes.Contains(connection.endNode))
                {
                    finalNodes.Add(connection.endNode);
                }
            }
        }

        return finalNodes;
    }

    public static List<Structure_Connection> MinimunSpanningTree(List<Structure_Node> nodes)
    {
        //  PrimsAlgorithm

        //  Starts at a random node, creates connections trough that node, taking the cheapes path,
        //  that paths end node gets added to list of nodes, and its connections, and loop continues untill all nodes are visited

        List<Structure_Connection> potentialConnections = new List<Structure_Connection>();
        List<Structure_Connection> finalConnections = new List<Structure_Connection>();

        Structure_Node startNode = nodes[0];
        startNode.SetBad();

        foreach (Structure_Node endNode in startNode.connectedNodes)
        {
            potentialConnections.Add(new Structure_Connection(startNode, endNode));
        }

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            Structure_Connection shortest = null;
            float distance = float.MaxValue;

            foreach (Structure_Connection potentialConnection in potentialConnections)
            {
                if (!potentialConnection.endNode.isBad)
                {
                    float tempDistance = Vector2.Distance(potentialConnection.startNode.position, potentialConnection.endNode.position);

                    if (tempDistance < distance)
                    {
                        shortest = potentialConnection;
                        distance = tempDistance;
                    }
                }
            }

            shortest.endNode.SetBad();
            finalConnections.Add(shortest);
            potentialConnections.Remove(shortest);

            foreach (Structure_Node node in shortest.endNode.connectedNodes)
            {
                if (!node.Equals(shortest.startNode))
                {
                    potentialConnections.Add(new Structure_Connection(shortest.endNode, node));
                }
            }
        }

        //  We add random connections
        //  We dont want duplicates so we say bye to those

        List<Vector2> occupied = new List<Vector2>();

        foreach (Structure_Connection potentialConnection in potentialConnections)
        {
            Vector2 pos = potentialConnection.startNode.position + potentialConnection.endNode.position;

            if (GetRandomValue(pos, 0, 10) == 0 && !occupied.Contains(pos))
            {
                finalConnections.Add(potentialConnection);
                occupied.Add(pos);
            }
        }

        return finalConnections;
    }

    public static Structure_Grid UpdateGridWithNewPath(Vector2 startPos, Vector2 endPos, Structure_Grid grid)
    {
        //  A*

        Structure_Grid_Node startSquare = grid.GetNodeFromWorldPosition(startPos);
        Structure_Grid_Node endSquare = grid.GetNodeFromWorldPosition(endPos);

        List<Structure_Grid_Node> openSet = new List<Structure_Grid_Node>();
        HashSet<Structure_Grid_Node> closedSet = new HashSet<Structure_Grid_Node>();

        openSet.Add(startSquare);
        startSquare.hCost = 0;
        startSquare.gCost = 0;

        while (openSet.Count > 0)
        {
            Structure_Grid_Node currentSquare = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentSquare.fCost || (openSet[i].fCost == currentSquare.fCost && openSet[i].hCost < currentSquare.hCost))
                {
                    currentSquare = openSet[i];
                }
            }

            openSet.Remove(currentSquare);
            closedSet.Add(currentSquare);

            if (currentSquare == endSquare)
            {
                RetracePath(startSquare, endSquare, grid);

                return grid;
            }

            foreach (Structure_Grid_Node neighbour in grid.GetNeighbours(currentSquare))
            {
                if (neighbour.isBad || closedSet.Contains(neighbour)) continue;

                int movementDistanceToNeighbour = currentSquare.gCost + currentSquare.GetDistanceToOtherNode(neighbour);

                if (movementDistanceToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = movementDistanceToNeighbour;
                    neighbour.hCost = neighbour.GetDistanceToOtherNode(endSquare);

                    neighbour.parent = currentSquare;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }

        Debug.LogError("End not found");

        return grid;
    }

    private static void RetracePath(Structure_Grid_Node start, Structure_Grid_Node end, Structure_Grid grid)
    {
        Structure_Grid_Node currentSquare = end;

        while (currentSquare != start)
        {
            grid.SetNodeToPath(currentSquare.position);
            currentSquare = currentSquare.parent;
        }
    }


    public static Vector2 GetRandom2DPosition(Vector2 position, int townSize, int index)
    {
        Vector2 newPositon;

        newPositon.x = position.x + (townSize - GetRandomValue(position, index + 33, townSize * 2));
        newPositon.y = position.y + (townSize - GetRandomValue(position, index + 66, townSize * 2));

        return newPositon;
    }

    public static Vector3 GetRandom3DPosition(Vector3 position, int townSize, int index)
    {
        Vector3 newPositon;

        Vector2 noisePosition = new Vector2(position.x + position.y, position.z + position.y);

        newPositon.x = position.x + (townSize - GetRandomValue(noisePosition, index + 33, townSize * 2));
        newPositon.y = position.y + (townSize - GetRandomValue(noisePosition, index + 66, townSize * 2));
        newPositon.z = position.z + (townSize - GetRandomValue(noisePosition, index + 99, townSize * 2));

        return newPositon;
    }

    public static int GetRandomValue(Vector2 noisePosition, int offset, int modulo)
    {
        //  First grabs some perlin to act as the noise
        //  Then does some mathematical maths on with that perlin,
        //  So that small changes in position, equal large changes in numbers
        //  Then gets the modulo of the value, to always return a value within range

        if (modulo == 0) return 0;

        float perlin = Mathf.PerlinNoise((noisePosition.x * 1111f) + offset * 1111f + 0.1f, (noisePosition.y * 1111f) + offset * 1111f + 0.1f);

        float value = (noisePosition.x * 1111f) + (noisePosition.y * 1111f) + perlin;
        value *= perlin;
        value *= value;

        value %= modulo;

        return Mathf.FloorToInt(value);
    }
}
