using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateTown
{
    public static Tuple<Structure, Vector2>[] GetTown(Town town)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();

        List<Vector2> roadPoints = new List<Vector2>();

        foreach(Tuple<Structure, Vector2> structure in GetTownCenter(town))
        {
            structures.Add(structure);
        }

        foreach (Tuple<Structure, Vector2> structure in GetHouses(town))
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

    public static Tuple<Structure, Vector2>[] GetTownCenter(Town town)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();

        structures.Add(Tuple.Create(town.townCenter[0], new Vector2(0, 0)));

        return structures.ToArray();
    }

    public static Tuple<Structure, Vector2>[] GetHouses(Town town)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();

        structures.Add(Tuple.Create(town.houses[0], new Vector2(20, 20)));

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
}
