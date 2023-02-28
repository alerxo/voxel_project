using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateDungeon
{
    public static void GenerateDungeonData(DungeonData dungeonData)
    {
        for (int i = 0; i < dungeonData.levels; i++)
        {
            dungeonData.roomNodes = new List<Vector2>();

            int yPosition = Mathf.FloorToInt(dungeonData.dungeonPosition.y) - (i * dungeonData.dungeon.dungeonLevelGap);

            if (i == dungeonData.dungeonEntranceLevel)
            {
                GetEntrance(dungeonData, yPosition);
            }

            if(i >= dungeonData.dungeonMinLevel)
            {
                GetRooms(dungeonData, yPosition);
            }

            if(dungeonData.roomNodes.Count > 1)
            {
                GetHallway(dungeonData, yPosition);
            }
        }
    }

    private static void GetEntrance(DungeonData dungeonData, int yPosition)
    {
        Vector3 position = new Vector3(dungeonData.dungeonEntrancePosition.x, yPosition, dungeonData.dungeonEntrancePosition.z);

        Structure structure = dungeonData.dungeon.entrances[Structure_Util.GetRandomValue(position, 0, dungeonData.dungeon.entrances.Length)];

        dungeonData.AddStructure(structure, position);
    }

    private static void GetRooms(DungeonData dungeonData, int yPosition)
    {
        //  Picks random positions for the house structures, if position doesnt overlap, place it

        int roomAmount = Mathf.FloorToInt(dungeonData.sizeRadius * dungeonData.density);
        
        for (int i = 0; i < roomAmount; i++)
        {
            Vector3 position = Structure_Util.GetRandom3DPosition(dungeonData.dungeonPosition, dungeonData.sizeRadius, i);

            position.y = yPosition;

            Structure structure = dungeonData.dungeon.rooms[Structure_Util.GetRandomValue(position, i, dungeonData.dungeon.rooms.Length)];

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

        List<Structure_Node> triangulationConnections = Structure_Util.Triangulation(dungeonData.dungeonPosition2, dungeonData.sizeRadius, dungeonData.roomNodes);
        List<Structure_Connection> minimunSpanningTreeConnections = Structure_Util.MinimunSpanningTree(triangulationConnections);

        GetHallwayData(dungeonData, minimunSpanningTreeConnections, yPosition);
    }

    private static void GetHallwayData(DungeonData dungeonData, List<Structure_Connection> connections, int yPosition)
    {
        //  Has a grid of squares, for each road connection, use a* to get block data
        //  Then chose random road id and create vm

        Structure_Grid grid = new Structure_Grid(dungeonData.dungeonPosition2, dungeonData.sizeRadius);

        foreach (Structure_Connection connection in connections)
        {
            grid = Structure_Util.UpdateGridWithNewPath(connection.startNode.position, connection.endNode.position, grid);
        }

        foreach (Vector2 roadblockPosition in grid.GetPathPositions())
        {
            byte id = 0;
            dungeonData.AddHallway(id, roadblockPosition, yPosition);
        }

        dungeonData.CompleteHallway();
    }

}
