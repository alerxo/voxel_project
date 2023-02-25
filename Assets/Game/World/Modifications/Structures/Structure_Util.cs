using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure_Util
{
    public static List<Tuple<Structure, Vector3>> structureList = new List<Tuple<Structure, Vector3>>();

    public static void DrawZone(Structure structure, Vector3 positon)
    {
        Vector3[] zonePoints = structure.GetZone();
        Vector3 blockOffset = new Vector3(0.5f, 0.5f, 0.5f) + positon;

        Vector3 xp = new Vector3(zonePoints[0].x + structure.zoneMargin, 0, 0);
        Vector3 yp = new Vector3(0, zonePoints[0].y + structure.zoneMargin, 0);
        Vector3 zp = new Vector3(0, 0, zonePoints[0].z + structure.zoneMargin);
        Vector3 xn = new Vector3(zonePoints[1].x - structure.zoneMargin, 0, 0);
        Vector3 yn = new Vector3(0, zonePoints[1].y - structure.zoneMargin, 0);
        Vector3 zn = new Vector3(0, 0, zonePoints[1].z - structure.zoneMargin);

        Debug.DrawLine(xp + yn + zp + blockOffset, xp + yp + zp + blockOffset, Color.white, 10f);
        Debug.DrawLine(xn + yn + zp + blockOffset, xn + yp + zp + blockOffset, Color.white, 10f);
        Debug.DrawLine(xp + yn + zn + blockOffset, xp + yp + zn + blockOffset, Color.white, 10f);
        Debug.DrawLine(xn + yn + zn + blockOffset, xn + yp + zn + blockOffset, Color.white, 10f);

        Debug.DrawLine(xp + yp + zp + blockOffset, xp + yp + zn + blockOffset, Color.white, 10f);
        Debug.DrawLine(xp + yn + zp + blockOffset, xp + yn + zn + blockOffset, Color.white, 10f);

        Debug.DrawLine(xn + yp + zp + blockOffset, xn + yp + zn + blockOffset, Color.white, 10f);
        Debug.DrawLine(xn + yn + zp + blockOffset, xn + yn + zn + blockOffset, Color.white, 10f);

        Debug.DrawLine(xp + yp + zp + blockOffset, xn + yp + zp + blockOffset, Color.white, 10f);
        Debug.DrawLine(xp + yn + zp + blockOffset, xn + yn + zp + blockOffset, Color.white, 10f);

        Debug.DrawLine(xp + yp + zn + blockOffset, xn + yp + zn + blockOffset, Color.white, 10f);
        Debug.DrawLine(xp + yn + zn + blockOffset, xn + yn + zn + blockOffset, Color.white, 10f);
    }   

    public static bool CheckOverlap(Structure structure, Vector3 position, bool add)
    {
        if(structureList.Count == 0)
        {
            if (add)
            {
                structureList.Add(Tuple.Create(structure, position));
                return false;
            }
        }

        foreach (Tuple<Structure, Vector3> s in structureList)
        {
            if(Vector3.Distance(position, s.Item2) < (structure.buildRadius + s.Item1.buildRadius))
            {
                return true;
            }
        }

        if (add)
        {
            structureList.Add(Tuple.Create(structure, position));
        }

        return false;
    }

    public static bool CheckOverlap(Vector3 position)
    {
        foreach (Tuple<Structure, Vector3> s in structureList)
        {
            if (Vector3.Distance(position, s.Item2) < s.Item1.buildRadius)
            {
                return true;
            }
        }

        return false;
    }

    public static bool CheckOverlap(Vector2 position)
    {
        foreach (Tuple<Structure, Vector3> s in structureList)
        {
            if (Vector2.Distance(position, new Vector2(s.Item2.x, s.Item2.z)) < s.Item1.buildRadius)
            {
                return true;
            }
        }

        return false;
    }
}
