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

        GetRooms(dungeonData);
    }

    private static void GetRooms(DungeonData dungeonData)
    {
        //  Picks random positions for the house structures, if position doesnt overlap, place it

        for (int l = 0; l < dungeonData.levels; l++)
        {
            int roomAmount = Mathf.FloorToInt(dungeonData.sizeRadius * dungeonData.density);
            int yPosition = Mathf.FloorToInt(dungeonData.dungeonPosition.y) - (l * dungeonData.dungeon.dungeonLevelGap);

            for (int r = 0; r < roomAmount; r++)
            {
                Vector3 position = GetRandomPosition(dungeonData.dungeonPosition, dungeonData.sizeRadius, r, yPosition);

                Structure structure = dungeonData.dungeon.rooms[GetRandomValue(position, r, dungeonData.dungeon.rooms.Length)];

                if (!Structure_Util.CheckOverlap(structure, position, true))
                {
                    dungeonData.AddStructure(structure, position);
                }
            }
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
