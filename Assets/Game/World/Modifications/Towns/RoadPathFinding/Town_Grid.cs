using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_Grid
{
    private Town_Square[,] grid;

    private Vector2 position;

    private List<Vector2> roadPositions = new List<Vector2>();

    public Town_Grid(Vector2 _position, int xSize, int ySize)
    {
        position = _position;

        grid = new Town_Square[xSize, ySize];

        Vector2 bottomLeftCornerPos = new Vector2(position.x - (xSize / 2), position.x - (xSize / 2));

        for(int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                Vector2 pos = new Vector2(x + bottomLeftCornerPos.x, y + bottomLeftCornerPos.y);
                bool isBad = Structure_Util.CheckOverlap(pos);

                grid[x, y] = new Town_Square(isBad, pos);
            }
        }

    }

    public void SetSquareToRoad(Vector2 pos)
    {
        int x = Mathf.FloorToInt(pos.x - position.x);
        int y = Mathf.FloorToInt(pos.y - position.y);

        grid[x, y].isRoad = true;

        roadPositions.Add(new Vector2(x, y));
    }

    public List<Vector2> GetRoadPositions()
    {
        return roadPositions;
    }
}
