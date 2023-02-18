using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateTown
{
    public static Tuple<Structure, Vector2>[] GetTown(Town town, Vector3 pos)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();
        List<Vector2> roadPoints = new List<Vector2>();

        foreach(Tuple<Structure, Vector2> structure in GetTownCenter(town, pos))
        {
            structures.Add(structure);
        }

        foreach (Tuple<Structure, Vector2> structure in GetHouses(town, pos))
        {
            structures.Add(structure);
            roadPoints.Add(structure.Item2);
        }

        foreach (Tuple<Structure, Vector2> structure in GetRoads(town, roadPoints))
        {
            structures.Add(structure);
        }

        return structures.ToArray();
    }

    public static Tuple<Structure, Vector2>[] GetTownCenter(Town town, Vector3 pos)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();

        structures.Add(Tuple.Create(town.townCenter[GetRandomValue(pos, 0, town.houses.Length)], new Vector2(pos.x, pos.z)));

        return structures.ToArray();
    }

    public static Tuple<Structure, Vector2>[] GetHouses(Town town, Vector3 pos)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();

        int townSize = town.townSize + (town.townSizeVariation - GetRandomValue(pos, 1, town.townSizeVariation * 2));
        float townDensity = town.townDensity + (town.townDensityVariation - ((float)GetRandomValue(pos, 2, Mathf.FloorToInt(town.townDensityVariation * town.townSize)) / (float)town.townSize));

        int houseAmount = Mathf.FloorToInt(townSize * townDensity);

        for(int i = 0; i < houseAmount; i++)
        {
            Vector2 housePos = GetRandomPos(new Vector2(pos.x, pos.z), townSize, i);
            Structure house = town.houses[GetRandomValue(housePos, i, town.houses.Length)];

            if(!Structure_Util.CheckOverlap(house, new Vector3(housePos.x, pos.y, housePos.y), true))
            {
                structures.Add(Tuple.Create(house, housePos));
            }
        }

        return structures.ToArray();
    }

    public static Tuple<Structure, Vector2>[] GetRoads(Town town, List<Vector2> roadPoints)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();

        foreach(Vector2 point in roadPoints)
        {
            structures.Add(Tuple.Create(town.roads[0], point));
        }

        return structures.ToArray();
    }

    private static Vector2 GetRandomPos(Vector2 pos, int townSize, int index)
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
