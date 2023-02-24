using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateTown
{
    public static TownData GetTownData(TownData townData)
    {
        List<Vector2> roadPoints = new List<Vector2>();

        foreach(Tuple<Structure, Vector2> structure in GetTownCenter(townData))
        {
            townData.structures.Add(structure);
            roadPoints.Add(structure.Item2);
        }

        foreach (Tuple<Structure, Vector2> structure in GetHouses(townData))
        {
            townData.structures.Add(structure);
            roadPoints.Add(structure.Item2);
        }

        townData.roadData = GetRoads(townData, roadPoints);

        return townData;
    }

    private static Tuple<Structure, Vector2>[] GetTownCenter(TownData townData)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();

        structures.Add(Tuple.Create(townData.town.townCenter[GetRandomValue(townData.position, 0, townData.town.houses.Length)], new Vector2(townData.position.x, townData.position.z)));

        return structures.ToArray();
    }

    private static Tuple<Structure, Vector2>[] GetHouses(TownData townData)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();

        int townSize = townData.town.townSize + (townData.town.townSizeVariation - GetRandomValue(townData.position, 1, townData.town.townSizeVariation * 2));
        float townDensity = townData.town.townDensity + (townData.town.townDensityVariation - ((float)GetRandomValue(townData.position, 2, Mathf.FloorToInt(townData.town.townDensityVariation * townData.town.townSize)) / (float)townData.town.townSize));

        int houseAmount = Mathf.FloorToInt(townSize * townDensity);

        for(int i = 0; i < houseAmount; i++)
        {
            Vector2 housePos = GetRandomPosition(new Vector2(townData.position.x, townData.position.z), townSize, i);
            Structure house = townData.town.houses[GetRandomValue(housePos, i, townData.town.houses.Length)];

            if(!Structure_Util.CheckOverlap(house, new Vector3(housePos.x, townData.position.y, housePos.y), true))
            {
                structures.Add(Tuple.Create(house, housePos));
            }
        }

        return structures.ToArray();
    }

    private static Queue<VoxelModification> GetRoads(TownData townData, List<Vector2> roadNodes)
    {
        List<Town_Node> roadConnections = BowyerWatson(townData, roadNodes);

        List<Town_Connection> roadConnection = PrimsAlgorithm(roadConnections);

        foreach(Town_Connection connection in roadConnection)
        {
            Debug.DrawLine(new Vector3(connection.startNode.position.x, 100f, connection.startNode.position.y), new Vector3(connection.endNode.position.x, 100f, connection.endNode.position.y), Color.green, 100f);
        }

        return GetRoadData(townData, roadConnection);
    }

    private static List<Town_Node> BowyerWatson(TownData townData, List<Vector2> nodePoints)
    {
        List<Town_Triangle> triangles = new List<Town_Triangle>();

        Town_Node superPointA = new Town_Node(townData.position2 + new Vector2(0, townData.town.townSize * 3.2f));
        Town_Node superPointB = new Town_Node(townData.position2 + new Vector2(-townData.town.townSize * 2.2f, -townData.town.townSize * 1.2f));
        Town_Node superPointC = new Town_Node(townData.position2 + new Vector2(townData.town.townSize * 2.2f, -townData.town.townSize * 1.2f));

        superPointA.SetBad();
        superPointB.SetBad();
        superPointC.SetBad();

        triangles.Add(new Town_Triangle(superPointA, superPointB, superPointC));

        foreach (Vector2 nodePoint in nodePoints)
        {
            List<Town_Connection> connections = new List<Town_Connection>();

            foreach (Town_Triangle triangle in triangles)
            {
                if (triangle.IsNodeInsideCircumcirlce(nodePoint))
                {
                    triangle.SetBad();

                    foreach (Town_Connection connection in triangle.GetConnections())
                    {
                        connections.Add(connection);
                    }
                }
            }

            triangles.RemoveAll((Town_Triangle t) => t.isBad);

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

            connections.RemoveAll((Town_Connection e) => e.isBad);

            Town_Node newNode = new Town_Node(nodePoint);

            foreach (Town_Connection connection in connections)
            {
                triangles.Add(new Town_Triangle(newNode, connection.startNode, connection.endNode));

                Town_Node.ConnectNodes(newNode, connection.startNode, connection.endNode);
            }
        }

        triangles.RemoveAll((Town_Triangle t) => t.ContainsNode(superPointA) || t.ContainsNode(superPointB) || t.ContainsNode(superPointC));

        List<Town_Connection> finalConnections = new List<Town_Connection>();
        List<Town_Node> finalNodes = new List<Town_Node>();
                
        foreach(Town_Triangle triangle in triangles)
        {
            foreach(Town_Connection connection in triangle.GetConnections())
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

    private static List<Town_Connection> PrimsAlgorithm(List<Town_Node> nodes)
    {
        List<Town_Connection> potentialConnections = new List<Town_Connection>();
        List<Town_Connection> finalConnections = new List<Town_Connection>();

        Town_Node startNode = nodes[0];
        startNode.SetBad();

        foreach (Town_Node endNode in startNode.connectedNodes)
        {
            potentialConnections.Add(new Town_Connection(startNode, endNode));
        }

        for(int i = 0; i < nodes.Count - 1; i++)
        {
            Town_Connection shortest = null;
            float distance = float.MaxValue;

            foreach (Town_Connection potentialConnection in potentialConnections)
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

            foreach (Town_Node node in shortest.endNode.connectedNodes)
            {
                potentialConnections.Add(new Town_Connection(shortest.endNode, node));
            }
        }

        foreach (Town_Connection potentialConnection in potentialConnections)
        {
            Vector2 pos = potentialConnection.startNode.position + potentialConnection.endNode.position;

            if(GetRandomValue(pos, 0, 10) == 0)
            {
                finalConnections.Add(potentialConnection);
            }
        }

        return finalConnections;
    }

    private static Queue<VoxelModification> GetRoadData(TownData townData,  List<Town_Connection> connections)
    {
        Town_Grid grid = new Town_Grid(townData.position2, townData.town.townSize * 2, townData.town.townSize * 2);

        foreach (Town_Connection connection in connections)
        {
            grid = UpdateGridWithAStar(connection.startNode.position, connection.endNode.position, grid);
        }

        Queue<VoxelModification> roadModifications = new Queue<VoxelModification>();

        foreach (Vector2 roadblockPosition in grid.GetRoadPositions())
        {
            byte id = townData.town.roadIds[GetRandomValue(roadblockPosition, 0, townData.town.roadIds.Length)];
            roadModifications.Enqueue(new VoxelModification(new Vector3(roadblockPosition.x - townData.position.x, townData.position.y, roadblockPosition.y - townData.position.z), id));
        }

        return roadModifications;
    }

    private static Town_Grid UpdateGridWithAStar(Vector2 startPos, Vector2 endPos, Town_Grid grid)
    {
        //grid.SetSquareToRoad(startPos);

        return grid;
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
        //  Then gets the modulo of the value, divided by the modulo as to always return a value between 0 and 1

        float perlin = Mathf.PerlinNoise((pos.x * 1111f) + offset * 1111f + 0.1f, (pos.y * 1111f) + offset * 1111f + 0.1f);

        float value = (pos.x * 1111f) + (pos.y * 1111f) + perlin;
        value *= perlin;
        value *= value;

        value %= largest;

        return Mathf.FloorToInt(value);
    }
}