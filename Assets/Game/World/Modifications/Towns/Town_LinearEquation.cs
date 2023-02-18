using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Town_LinearEquation
{
    public float a;
    public float b;
    public float c;

    public Town_LinearEquation()
    {

    }

    public Town_LinearEquation(Vector2 pointA, Vector2 pointB)
    {
        float deltaX = pointB.x - pointA.x;
        float deltaY = pointB.y - pointA.y;

        a = deltaY;
        b = -deltaX;
        c = a * pointA.x + b * pointA.y;
    }

    public Town_LinearEquation PerpendicularLineAt(Vector2 point)
    {
        Town_LinearEquation newLine = new Town_LinearEquation();

        newLine.a = -b;
        newLine.b = a;
        newLine.c = newLine.a * point.x + newLine.b * point.y;

        return newLine;
    }
}
