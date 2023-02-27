using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure_Grid
{
    private Structure_Grid_Node[,] grid;

    private Vector2 position;

    private int gridRadius;

    private List<Vector2> pathPositions = new List<Vector2>();

    public Structure_Grid(Vector2 _position, int _gridRadius)
    {
        position = _position;
        gridRadius = _gridRadius;

        grid = new Structure_Grid_Node[gridRadius * 2, gridRadius * 2];

        for (int x = 0; x < gridRadius * 2; x++)
        {
            for (int y = 0; y < gridRadius * 2; y++)
            {
                Vector2 pos = new Vector2(x, y);
                bool isBad = Structure_Util.CheckOverlap(pos);

                grid[x, y] = new Structure_Grid_Node(isBad, pos);
            }
        }
    }

    public List<Structure_Grid_Node> GetNeighbours(Structure_Grid_Node node)
    {
        List<Structure_Grid_Node> neighbours = new List<Structure_Grid_Node>();

        for(int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                float tempX = node.position.x + x;
                float tempY = node.position.y + y;

                if (tempX >= 0 && tempX < gridRadius * 2 && tempY >= 0 && tempY < gridRadius * 2)
                {
                    neighbours.Add(grid[(int)tempX, (int)tempY]);
                }
            }
        }

        return neighbours;
    }

    public Structure_Grid_Node GetNodeFromWorldPosition(Vector2 pos)
    {
        int x = Mathf.FloorToInt(pos.x - position.x) + gridRadius;
        int y = Mathf.FloorToInt(pos.y - position.y) + gridRadius;

        return grid[x, y];
    }

    public void SetNodeToPath(Vector2 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);

        grid[x, y].isPath = true;

        pathPositions.Add(new Vector2(x - gridRadius, y - gridRadius));
    }

    public List<Vector2> GetPathPositions()
    {
        return pathPositions;
    }
}
