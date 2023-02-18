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

        foreach (Tuple<Structure, Vector2> structure in GetRoads(town, roadPoints, new Vector2(pos.x, pos.z)))
        {
            structures.Add(structure);
        }

        return structures.ToArray();
    }

    private static Tuple<Structure, Vector2>[] GetTownCenter(Town town, Vector3 pos)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();

        structures.Add(Tuple.Create(town.townCenter[GetRandomValue(pos, 0, town.houses.Length)], new Vector2(pos.x, pos.z)));

        return structures.ToArray();
    }

    private static Tuple<Structure, Vector2>[] GetHouses(Town town, Vector3 pos)
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

    private static Tuple<Structure, Vector2>[] GetRoads(Town town, List<Vector2> roadPoints, Vector2 pos)
    {
        List<Tuple<Structure, Vector2>> structures = new List<Tuple<Structure, Vector2>>();

        List<Town_Triangle> triangles = BowyerWatson(town, roadPoints, pos);

        foreach(Town_Triangle triangle in triangles)
        {
            foreach(Town_Edge edge in triangle.GetEdges())
            {
                Debug.DrawLine(new Vector3(edge.pointA.x, 100f, edge.pointA.y), new Vector3(edge.pointB.x, 100f, edge.pointB.y), Color.white, 100f);
            }
        }

        return structures.ToArray();
    }



    private static List<Town_Triangle> BowyerWatson(Town town, List<Vector2> points, Vector2 pos)
    {
        List<Town_Triangle> triangles = new List<Town_Triangle>();

        Vector2 orgPointA = pos + new Vector2(0, town.townSize * 3.2f);
        Vector2 orgPointB = pos + new Vector2(-town.townSize * 2.2f, -town.townSize * 1.2f);
        Vector2 orgPointC = pos + new Vector2(town.townSize * 2.2f, -town.townSize * 1.2f);

        triangles.Add(new Town_Triangle(orgPointA, orgPointB, orgPointC));

        foreach(Vector2 point in points)
        {
            List<Town_Triangle> badTriangles = new List<Town_Triangle>();

            foreach(Town_Triangle triangle in triangles)
            {
                if (IsPointInsideCircumcirlce(point, triangle))
                {
                    badTriangles.Add(triangle);
                }
            }

            List<Town_Edge> edges = new List<Town_Edge>();

            foreach (Town_Triangle triangle in badTriangles)
            {
                foreach(Town_Edge edge in triangle.GetEdges())
                {
                    if (EdgeIsNotShared(badTriangles, triangle, edge))
                    {
                        edges.Add(edge);
                    }
                }
            }
        

            foreach (Town_Triangle triangle in badTriangles)
            {
                triangles.Remove(triangle);
            }

            foreach (Town_Edge edge in edges)
            {
                triangles.Add(new Town_Triangle(edge.pointA, edge.pointB, point));
            }
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            Town_Triangle triangle = triangles[i];

            if(triangle.ContainsPoint(orgPointA) || triangle.ContainsPoint(orgPointB) || triangle.ContainsPoint(orgPointC))
            {
                triangles.Remove(triangle);
                i--;
            }
        }

        return triangles;
    }

    private static bool IsPointInsideCircumcirlce(Vector2 point, Town_Triangle triangle)
    {
        Vector2 a = triangle.pointA;
        Vector2 b = triangle.pointB;
        Vector2 c = triangle.pointC;

        float d = ((a.x * (b.y - c.y)) + (b.x * (c.y - a.y)) + (c.x * (a.y - b.y))) * 2f;

        float x = (1f / d) * (((Mathf.Pow(a.x, 2) + Mathf.Pow(a.y, 2)) * (b.y - c.y)) + ((Mathf.Pow(b.x, 2) + Mathf.Pow(b.y, 2)) * (c.y - a.y)) + ((Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2)) * (a.y - b.y)));
        float y = (1f / d) * (((Mathf.Pow(a.x, 2) + Mathf.Pow(a.y, 2)) * (c.x - b.x)) + ((Mathf.Pow(b.x, 2) + Mathf.Pow(b.y, 2)) * (a.x - c.x)) + ((Mathf.Pow(c.x, 2) + Mathf.Pow(c.y, 2)) * (b.x - a.x)));

        Vector2 circumcentre = new Vector2(x, y);

        float radius = Vector2.Distance(b, circumcentre);
       
        if(Vector2.Distance(point, circumcentre) <= radius)
        {
            return true;
        }

        return false;
    }

    private static bool IsPointInsideCircumcirlcew(Vector2 point, Town_Triangle triangle)
    {
        Vector2 a = triangle.pointA;
        Vector2 b = triangle.pointB;
        Vector2 c = triangle.pointC;

        float ab = Vector2.Distance(a, b);
        float bc = Vector2.Distance(b, c);
        float ca = Vector2.Distance(c, a);

        float radius = (ab * bc * ca) / Mathf.Sqrt((ab + bc + ca) * (bc + ca - ab) * (ca + ab - bc) * (ab + bc - ca));


        Town_LinearEquation lineAB = new Town_LinearEquation(a, b);
        Town_LinearEquation lineBC = new Town_LinearEquation(b, c);

        Vector2 midPointAB = Vector2.Lerp(a, b, 0.5f);
        Vector2 midPointBC = Vector2.Lerp(b, c, 0.5f);

        

        //  

        Town_LinearEquation perpendicularAB = lineAB.PerpendicularLineAt(midPointAB);
        Town_LinearEquation perpendicularBC = lineBC.PerpendicularLineAt(midPointBC);

        Vector2 circumcenter = GetCrossingPoint(perpendicularAB, perpendicularBC);

        if(Vector2.Distance(point, circumcenter) <= radius)
        {
            Debug.Log("true");
            Debug.Log(point);
            Debug.Log(circumcenter);
            Debug.Log(radius);

            Debug.Log(" ");

            Debug.Log(a);
            Debug.Log(b);
            Debug.Log(c);
            return true;
        }

        Debug.Log("false");
        Debug.Log(point);
        Debug.Log(circumcenter);
        Debug.Log(radius);

        Debug.Log(" ");

        Debug.Log(a);
        Debug.Log(b);
        Debug.Log(c);

        return false;
    }

    private static Vector2 GetCrossingPoint(Town_LinearEquation lineA, Town_LinearEquation lineB)
    {
        float a1 = lineA.a;
        float a2 = lineB.a;
        float b1 = lineA.b;
        float b2 = lineB.b;
        float c1 = lineA.c;
        float c2 = lineB.c;

        float determinant = a1 * b2 - a2 * b1;
        float determinantX = c1 * b2 - c2 * b1;
        float determinantY = a1 * c2 - a2 * c1;

        float x = determinantX / determinant;
        float y = determinantY / determinant;

        return new Vector2(x, y);
    }

    private static bool EdgeIsNotShared(List<Town_Triangle> badTriangles, Town_Triangle currentTrianlge, Town_Edge edge)
    {
        foreach (Town_Triangle triangle in badTriangles)
        {
            if(triangle != currentTrianlge)
            {
                foreach (Town_Edge badEdge in triangle.GetEdges())
                {
                    if (Vector2.Distance(edge.pointA, badEdge.pointA) < 0.1f  && Vector2.Distance(edge.pointB, badEdge.pointB) < 0.1f)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
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