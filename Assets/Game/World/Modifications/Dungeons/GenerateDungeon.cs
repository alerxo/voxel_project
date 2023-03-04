using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateDungeon
{
    public static void GenerateDungeonData(DungeonData dungeonData)
    {
        dungeonData.nextYPosition = Mathf.FloorToInt(dungeonData.position3D.y);

        for (int i = 0; i < dungeonData.levels; i++)
        {
            dungeonData.roomNodes = dungeonData.cachedRoomNodes;
            dungeonData.cachedRoomNodes = new List<Vector2>();

            int yPosition = dungeonData.nextYPosition;

            if (i == 0)
            {
                GetEntrance(dungeonData, yPosition);
            }

            else
            {
                if (i < dungeonData.levels - 1)
                {
                    GetStairs(dungeonData, yPosition, i);
                }

                GetRooms(dungeonData, yPosition);
            }

            if (dungeonData.roomNodes.Count > 1)
            {
                GetHallway(dungeonData, yPosition);
            }
        }
    }

    private static void GetEntrance(DungeonData dungeonData, int yPosition)
    {
        Vector3 position = new Vector3(dungeonData.entrancePosition.x, yPosition, dungeonData.entrancePosition.z);

        Structure structure = dungeonData.dungeon.entrances[Structure_Util.GetRandomValue(position, 0, dungeonData.dungeon.entrances.Length)];

        dungeonData.nextYPosition += Mathf.FloorToInt(structure.entrances[1].y);
        dungeonData.cachedRoomNodes.Add(new Vector2(position.x + structure.entrances[1].x, position.z + structure.entrances[1].z));

        dungeonData.AddStructure(structure, position);
    }

    private static void GetStairs(DungeonData dungeonData, int yPosition, int index)
    {
        Vector3 position = dungeonData.position3D;
        position.y = yPosition;
        position = Structure_Util.GetRandom3DPosition(position, dungeonData.size, -index);
        position.y = yPosition;

        Structure structure = dungeonData.dungeon.stairs[Structure_Util.GetRandomValue(position, index, dungeonData.dungeon.stairs.Length)];

        dungeonData.nextYPosition += Mathf.FloorToInt(structure.entrances[1].y);
        dungeonData.cachedRoomNodes.Add(new Vector2(position.x + structure.entrances[1].x, position.z + structure.entrances[1].z));

        dungeonData.AddStair(structure, position);
    }

    private static void GetRooms(DungeonData dungeonData, int yPosition)
    {
        //  Picks random positions for the house structures, if position doesnt overlap, place it

        int roomAmount = Mathf.FloorToInt(dungeonData.size * dungeonData.density);
        
        for (int i = 0; i < roomAmount; i++)
        {
            Vector3 position = dungeonData.position3D;
            position.y = yPosition;
            position = Structure_Util.GetRandom3DPosition(position, dungeonData.size, i);
            position.y = yPosition;

            Structure structure = dungeonData.dungeon.rooms[Structure_Util.GetRandomValue(position, i, dungeonData.dungeon.rooms.Length)];

            dungeonData.AddStructure(structure, position);
        }
    }

    private static void GetHallway(DungeonData dungeonData, int yPosition)
    {
        //  Gets a network of roads connecting the houses
        //  Uses delaunay, boywer watson algorithm to get all connections between houses
        //  Then uses prims algorithm to get a minimum spanning tree
        //  Then uses a* to place the road blocks

        List<Structure_Node> triangulationConnections = Structure_Util.Triangulation(dungeonData.position2D, dungeonData.size, dungeonData.roomNodes);
        List<Structure_Connection> minimunSpanningTreeConnections = Structure_Util.MinimunSpanningTree(triangulationConnections);

        if(minimunSpanningTreeConnections == null)
        {
            return;
        }

        GetHallwayData(dungeonData, minimunSpanningTreeConnections, yPosition);
    }

    private static void GetHallwayData(DungeonData dungeonData, List<Structure_Connection> connections, int yPosition)
    {
        //  Has a grid of squares, for each road connection, use a* to get block data
        //  Then chose random road id and create vm

        Structure_Grid grid = new Structure_Grid(dungeonData.position2D, dungeonData.size + 5);

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
