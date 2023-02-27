using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateTown
{
    public static void GenerateTownData(TownData townData)
    {
        //  Gets the data about the town and returns

        townData.sizeRadius = townData.town.townSize + (townData.town.townSizeVariation - GetRandomValue(townData.townPosition, 1, townData.town.townSizeVariation * 2));
        townData.density = townData.town.townDensity + (townData.town.townDensityVariation - ((float)GetRandomValue(townData.townPosition, 2, Mathf.FloorToInt(townData.town.townDensityVariation * townData.town.townSize)) / (float)townData.town.townSize));

        GetTownCenter(townData);
        GetHouses(townData);
        GetRoads(townData);
    }

    private static void GetTownCenter(TownData townData)
    {
        //  Picks one of the town center structures

        Vector2 position = new Vector2(townData.townPosition.x, townData.townPosition.z);
        Structure structure = townData.town.townCenter[GetRandomValue(position, 0, townData.town.townCenter.Length)];

        townData.AddStructure(structure, position);
    }

    private static void GetHouses(TownData townData)
    {
        //  Picks random positions for the house structures, if position doesnt overlap, place it

        int houseAmount = Mathf.FloorToInt(townData.sizeRadius * townData.density);

        for(int i = 0; i < houseAmount; i++)
        {
            Vector2 position = GetRandomPosition(townData.townPosition2, townData.sizeRadius, i);

            Structure structure = townData.town.houses[GetRandomValue(position, i, townData.town.houses.Length)];

            if(!Structure_Util.CheckOverlap(structure, new Vector3(position.x, townData.townPosition.y, position.y), true))
            {
                townData.AddStructure(structure, position);
            }
        }
    }

    private static void GetRoads(TownData townData)
    {
        //  Gets a network of roads connecting the houses
        //  Uses delaunay, boywer watson algorithm to get all connections between houses
        //  Then uses prims algorithm to get a minimum spanning tree
        //  Then uses a* to place the road blocks

        List<Structure_Node> triangulationConnections = RoadTriangulation(townData, townData.structureNodes);
        List<Structure_Connection> minimunSpanningTreeConnections = RoadMinimunSpanningTree(triangulationConnections);

        GetRoadData(townData, minimunSpanningTreeConnections);
    }

    private static List<Structure_Node> RoadTriangulation(TownData townData, List<Vector2> nodePoints)
    {
        //  BowyerWatson

        //  Starts by placing the super triangle and sets it bad as we dont want it later
        //  Then starts looping through each node, replacing the triangles when needed

        List<Structure_Triangle> triangles = new List<Structure_Triangle>();

        Structure_Node superPointA = new Structure_Node(townData.townPosition2 + new Vector2(0, townData.town.townSize * 3.2f));
        Structure_Node superPointB = new Structure_Node(townData.townPosition2 + new Vector2(-townData.town.townSize * 2.2f, -townData.town.townSize * 1.2f));
        Structure_Node superPointC = new Structure_Node(townData.townPosition2 + new Vector2(townData.town.townSize * 2.2f, -townData.town.townSize * 1.2f));

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

            for(int i = 0; i < connections.Count; i++)
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
                
        foreach(Structure_Triangle triangle in triangles)
        {
            foreach(Structure_Connection connection in triangle.GetConnections())
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

    private static List<Structure_Connection> RoadMinimunSpanningTree(List<Structure_Node> nodes)
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

        for(int i = 0; i < nodes.Count - 1; i++)
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

            if(GetRandomValue(pos, 0, 10) == 0 && !occupied.Contains(pos))
            {
                finalConnections.Add(potentialConnection);
                occupied.Add(pos);
            }
        }

        return finalConnections;
    }

    private static void GetRoadData(TownData townData,  List<Structure_Connection> connections)
    {
        //  Has a grid of squares, for each road connection, use a* to get block data
        //  Then chose random road id and create vm

        Structure_Grid grid = new Structure_Grid(townData.townPosition2, townData.sizeRadius);

        foreach (Structure_Connection connection in connections)
        {
            grid = UpdateRoadGridWithNewRoad(connection.startNode.position, connection.endNode.position, grid);
        }

        foreach (Vector2 roadblockPosition in grid.GetPathPositions())
        {
            byte id = townData.town.roadIds[GetRandomValue(roadblockPosition, 0, townData.town.roadIds.Length)];
            townData.AddRoad(id, roadblockPosition);        
        }
    }

    private static Structure_Grid UpdateRoadGridWithNewRoad(Vector2 startPos, Vector2 endPos, Structure_Grid grid)
    {
        //  A*

        Structure_Grid_Node startSquare = grid.GetNodeFromWorldPosition(startPos); 
        Structure_Grid_Node endSquare = grid.GetNodeFromWorldPosition(endPos);

        List<Structure_Grid_Node> openSet = new List<Structure_Grid_Node>();
        HashSet<Structure_Grid_Node> closedSet = new HashSet<Structure_Grid_Node>();

        openSet.Add(startSquare);
        startSquare.hCost = 0;
        startSquare.gCost = 0;

        while(openSet.Count > 0)
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

            if(currentSquare == endSquare)
            {
                RetracePath(startSquare, endSquare, grid);

                return grid;
            }

            foreach(Structure_Grid_Node neighbour in grid.GetNeighbours(currentSquare))
            {
                if (neighbour.isBad || closedSet.Contains(neighbour)) continue;

                int movementDistanceToNeighbour = currentSquare.gCost + currentSquare.GetDistanceToOtherNode(neighbour);

                if(movementDistanceToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
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

        while(currentSquare != start)
        {
            grid.SetNodeToPath(currentSquare.position);
            currentSquare = currentSquare.parent;
        }
    }


    private static Vector2 GetRandomPosition(Vector2 pos, int townSize, int index)
    {
        return new Vector2(pos.x + (townSize - GetRandomValue(pos, index, townSize * 2)), pos.y + (townSize - GetRandomValue(pos, index * 2, townSize * 2)));
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