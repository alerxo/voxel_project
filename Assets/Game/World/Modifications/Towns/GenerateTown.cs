using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateTown
{
    public static void GenerateTownData(TownData townData)
    {
        //  Gets the data about the town and returns

        townData.sizeRadius = townData.town.townSize + (townData.town.townSizeVariation - Structure_Util.GetRandomValue(townData.townPosition, 1, townData.town.townSizeVariation * 2));
        townData.density = townData.town.townDensity + (townData.town.townDensityVariation - ((float)Structure_Util.GetRandomValue(townData.townPosition, 2, Mathf.FloorToInt(townData.town.townDensityVariation * townData.town.townSize)) / (float)townData.town.townSize));

        GetTownCenter(townData);
        GetHouses(townData);
        GetRoads(townData);
    }

    private static void GetTownCenter(TownData townData)
    {
        //  Picks one of the town center structures

        Vector2 position = new Vector2(townData.townPosition.x, townData.townPosition.z);
        Structure structure = townData.town.townCenter[Structure_Util.GetRandomValue(position, 0, townData.town.townCenter.Length)];

        townData.AddStructure(structure, position);
    }

    private static void GetHouses(TownData townData)
    {
        //  Picks random positions for the house structures, if position doesnt overlap, place it

        int houseAmount = Mathf.FloorToInt(townData.sizeRadius * townData.density);

        for(int i = 0; i < houseAmount; i++)
        {
            Vector2 position = Structure_Util.GetRandom2DPosition(townData.townPosition2, townData.sizeRadius, i);

            Structure structure = townData.town.houses[Structure_Util.GetRandomValue(position, i, townData.town.houses.Length)];

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

        List<Structure_Node> triangulationConnections = Structure_Util.Triangulation(townData.townPosition2, townData.town.townSize, townData.structureNodes);
        List<Structure_Connection> minimunSpanningTreeConnections = Structure_Util.MinimunSpanningTree(triangulationConnections);

        GetRoadData(townData, minimunSpanningTreeConnections);
    }

    private static void GetRoadData(TownData townData,  List<Structure_Connection> connections)
    {
        //  Has a grid of squares, for each road connection, use a* to get block data
        //  Then chose random road id and create vm

        Structure_Grid grid = new Structure_Grid(townData.townPosition2, townData.sizeRadius);

        foreach (Structure_Connection connection in connections)
        {
            grid = Structure_Util.UpdateGridWithNewPath(connection.startNode.position, connection.endNode.position, grid);
        }

        foreach (Vector2 roadblockPosition in grid.GetPathPositions())
        {
            byte id = townData.town.roadIds[Structure_Util.GetRandomValue(roadblockPosition, 0, townData.town.roadIds.Length)];
            townData.AddRoad(id, roadblockPosition);
        }

        townData.CompleteRoads();
    }
}