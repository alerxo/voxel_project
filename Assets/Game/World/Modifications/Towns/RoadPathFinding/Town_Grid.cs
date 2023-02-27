using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Grid
{
    private Town_Square[,] grid;

    private Vector2 position;
    private Vector2 bottomLeftCornerPos;

    private int townSizeRadius;

    private List<Vector2> roadPositions = new List<Vector2>();

    public Town_Grid(Vector2 _position, int _townSizeRadius)
    {
        position = _position;
        townSizeRadius = _townSizeRadius;

        bottomLeftCornerPos = new Vector2(position.x - townSizeRadius, position.x - townSizeRadius);

        grid = new Town_Square[townSizeRadius * 2, townSizeRadius * 2];

        for (int x = 0; x < townSizeRadius * 2; x++)
        {
            for (int y = 0; y < townSizeRadius * 2; y++)
            {
                Vector2 pos = new Vector2(x, y);
                bool isBad = Structure_Util.CheckOverlap(pos);

                grid[x, y] = new Town_Square(isBad, pos);
            }
        }
    }

    public List<Town_Square> GetNeighbours(Town_Square town_Square)
    {
        List<Town_Square> neighbours = new List<Town_Square>();

        for(int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                float tempX = town_Square.position.x + x;
                float tempY = town_Square.position.y + y;

                if (tempX >= 0 && tempX <= townSizeRadius * 2 && tempY >= 0 && tempY <= townSizeRadius * 2)
                {
                    neighbours.Add(grid[(int)tempX, (int)tempY]);
                }
            }
        }

        return neighbours;
    }

    public Town_Square GetSquareFromWorldPosition(Vector2 pos)
    {
        int x = Mathf.FloorToInt(pos.x - position.x) + townSizeRadius;
        int y = Mathf.FloorToInt(pos.y - position.y) + townSizeRadius;

        return grid[x, y];
    }

    public void SetSquareToRoad(Vector2 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);

        grid[x, y].isRoad = true;

        roadPositions.Add(new Vector2(x - townSizeRadius, y - townSizeRadius));
    }

    public List<Vector2> GetRoadPositions()
    {
        return roadPositions;
    }
}
