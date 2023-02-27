using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateDungeon
{
    public static void GenerateDungeonData(DungeonData dungeonData)
    {
        dungeonData.levels = dungeonData.dungeon.dungeonLevelAmount + (dungeonData.dungeon.dungeonLevelAmountVariation - GetRandomValue(dungeonData.dungeonPosition, 1, dungeonData.dungeon.dungeonLevelAmountVariation * 2));
        dungeonData.sizeRadius = dungeonData.dungeon.dungeonSize + (dungeonData.dungeon.dungeonSizeVariation - GetRandomValue(dungeonData.dungeonPosition, 1, dungeonData.dungeon.dungeonSizeVariation * 2));
        dungeonData.density = dungeonData.dungeon.dungeonDensity + (dungeonData.dungeon.dungeonDensityVariation - ((float)GetRandomValue(dungeonData.dungeonPosition, 2, Mathf.FloorToInt(dungeonData.dungeon.dungeonDensityVariation * dungeonData.dungeon.dungeonSize)) / (float)dungeonData.dungeon.dungeonSize));


        for (int i = 0; i < dungeonData.levels; i++)
        {
            dungeonData.roomNodes = new List<Vector2>();

            int yPosition = Mathf.FloorToInt(dungeonData.dungeonPosition.y) - (i * dungeonData.dungeon.dungeonLevelGap);

            GetRooms(dungeonData, yPosition);
            GetHallway(dungeonData, yPosition);
        }
    }

    private static void GetRooms(DungeonData dungeonData, int yPosition)
    {
        //  Picks random positions for the house structures, if position doesnt overlap, place it

        int roomAmount = Mathf.FloorToInt(dungeonData.sizeRadius * dungeonData.density);
        
        for (int i = 0; i < roomAmount; i++)
        {
            Vector3 position = GetRandomPosition(dungeonData.dungeonPosition, dungeonData.sizeRadius, i, yPosition);

            Structure structure = dungeonData.dungeon.rooms[GetRandomValue(position, i, dungeonData.dungeon.rooms.Length)];

            if (!Structure_Util.CheckOverlap(structure, position, true))
            {
                dungeonData.AddStructure(structure, position);
            }
        }
    }

    private static void GetHallway(DungeonData dungeonData, int yPosition)
    {
        //  Gets a network of roads connecting the houses
        //  Uses delaunay, boywer watson algorithm to get all connections between houses
        //  Then uses prims algorithm to get a minimum spanning tree
        //  Then uses a* to place the road blocks

        List<Structure_Node> triangulationConnections = HallwayTriangulation(dungeonData, dungeonData.roomNodes);
        List<Structure_Connection> minimunSpanningTreeConnections = HallwayMinimunSpanningTree(triangulationConnections);

        GetHallwayData(dungeonData, minimunSpanningTreeConnections, yPosition);
    }

    private static List<Structure_Node> HallwayTriangulation(DungeonData dungeonData, List<Vector2> nodePoints)
    {
        //  BowyerWatson

        //  Starts by placing the super triangle and sets it bad as we dont want it later
        //  Then starts looping through each node, replacing the triangles when needed

        List<Structure_Triangle> triangles = new List<Structure_Triangle>();

        Structure_Node superPointA = new Structure_Node(dungeonData.dungeonPosition2 + new Vector2(0, dungeonData.dungeon.dungeonSize * 3.2f));
        Structure_Node superPointB = new Structure_Node(dungeonData.dungeonPosition2 + new Vector2(-dungeonData.dungeon.dungeonSize * 2.2f, -dungeonData.dungeon.dungeonSize * 1.2f));
        Structure_Node superPointC = new Structure_Node(dungeonData.dungeonPosition2 + new Vector2(dungeonData.dungeon.dungeonSize * 2.2f, -dungeonData.dungeon.dungeonSize * 1.2f));

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

    private static List<Structure_Connection> HallwayMinimunSpanningTree(List<Structure_Node> nodes)
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

    private static void GetHallwayData(DungeonData dungeonData, List<Structure_Connection> connections, int yPosition)
    {
        //  Has a grid of squares, for each road connection, use a* to get block data
        //  Then chose random road id and create vm

        Structure_Grid grid = new Structure_Grid(dungeonData.dungeonPosition2, dungeonData.sizeRadius);

        foreach (Structure_Connection connection in connections)
        {
            grid = UpdateRoadGridWithNewHallway(connection.startNode.position, connection.endNode.position, grid);
        }

        foreach (Vector2 roadblockPosition in grid.GetPathPositions())
        {
            byte id = 0;
            dungeonData.AddHallway(id, roadblockPosition, yPosition);
        }

        dungeonData.CompleteHallway();
    }

    private static Structure_Grid UpdateRoadGridWithNewHallway(Vector2 startPos, Vector2 endPos, Structure_Grid grid)
    {
        //  A*

        Structure_Grid_Node startNode = grid.GetNodeFromWorldPosition(startPos);
        Structure_Grid_Node endNode = grid.GetNodeFromWorldPosition(endPos);

        List<Structure_Grid_Node> openSet = new List<Structure_Grid_Node>();
        HashSet<Structure_Grid_Node> closedSet = new HashSet<Structure_Grid_Node>();

        openSet.Add(startNode);
        startNode.hCost = 0;
        startNode.gCost = 0;

        while (openSet.Count > 0)
        {
            Structure_Grid_Node currentNode = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == endNode)
            {
                RetracePath(startNode, endNode, grid);

                return grid;
            }

            foreach (Structure_Grid_Node neighbour in grid.GetNeighbours(currentNode))
            {
                if (neighbour.isBad || closedSet.Contains(neighbour)) continue;

                int movementDistanceToNeighbour = currentNode.gCost + currentNode.GetDistanceToOtherNode(neighbour);

                if (movementDistanceToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = movementDistanceToNeighbour;
                    neighbour.hCost = neighbour.GetDistanceToOtherNode(endNode);

                    neighbour.parent = currentNode;

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
        Structure_Grid_Node currentNode = end;

        while (currentNode != start)
        {
            grid.SetNodeToPath(currentNode.position);
            currentNode = currentNode.parent;
        }
    }


    private static Vector3 GetRandomPosition(Vector3 pos, int townSize, int index, int yPosition)
    {
        Vector2 positon2 = new Vector2(pos.x + (townSize - GetRandomValue(pos, index + yPosition, townSize * 2)), pos.z + (townSize - GetRandomValue(pos, (index + yPosition) * 2, townSize * 2)));

        return new Vector3(positon2.x, yPosition, positon2.y);
    }

    private static int GetRandomValue(Vector2 pos, int offset, int largest)
    {
        //  First grabs some perlin to act as the noise
        //  Then does some mathematical maths on with that perlin,
        //  So that small changes in position, equal large changes in numbers
        //  Then gets the modulo of the value, to always return a value within range

        if (largest == 0) return 0;

        float perlin = Mathf.PerlinNoise((pos.x * 1111f) + offset * 1111f + 0.1f, (pos.y * 1111f) + offset * 1111f + 0.1f);

        float value = (pos.x * 1111f) + (pos.y * 1111f) + perlin;
        value *= perlin;
        value *= value;

        value %= largest;

        return Mathf.FloorToInt(value);
    }
}
