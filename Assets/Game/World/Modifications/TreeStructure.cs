using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

[CreateAssetMenu(fileName = "Structure", menuName = "StructureMenu/TreeStructure")]
public class TreeStructure : ScriptableObject
{
    public float threshold;
    public float offset;

    public byte woodId;
    public byte leafId;

    [Space]

    public float colonialisationRadius;
    public float colonialisationHeight;

    public int colonialisationPointAmount;
    public int colonialisationStartHeight;

    public float branchGrowRangeToPoints;

    public float branchRadius;

    public float branchLength;


    public VoxelModification[] GetVoxels(Vector3 pos)
    {
        List<VoxelModification> voxels = new List<VoxelModification>();

        List<Tuple<Vector3, Vector3>> branches = GetBranches(new Vector2(pos.x, pos.z));

        for(int x = 0; x < colonialisationRadius * 2; x++)
        {
            for (int y = 0; y < colonialisationHeight; y++)
            {
                for (int z = 0; z < colonialisationRadius * 2; z++)
                {
                    Vector3 voxelPos = new Vector3(x, y, z);

                    foreach (Tuple<Vector3, Vector3> branch in branches)
                    {
                        if(DistanceToBranch(branch, voxelPos) < branchRadius)
                        {
                            voxels.Add(new VoxelModification(voxelPos, woodId));
                        }
                    }
                }
            }
        }

        return voxels.ToArray();
    }

    private float DistanceToBranch(Tuple<Vector3, Vector3> branch, Vector3 voxelPos)
    {
        return Vector3.Cross((branch.Item2 - branch.Item1).normalized, voxelPos - branch.Item1).magnitude;
    }


    private List<Tuple<Vector3, Vector3>> GetBranches(Vector2 pos2d)
    {
        List<Vector3> points = GetPoints(pos2d);

        List<Tuple<Vector3, Vector3>> branches = new List<Tuple<Vector3, Vector3>>();

        branches.Add(Tuple.Create(points[0], points[1]));
        branches.Add(Tuple.Create(points[2], points[3]));
        branches.Add(Tuple.Create(points[3], points[4]));
        branches.Add(Tuple.Create(points[5], points[6]));

        return branches;


        Queue<Vector3> growingBranches = new Queue<Vector3>();

        growingBranches.Enqueue(new Vector3(0, 0, 0));

        int failsafe = 0;

        while (growingBranches.Peek().y < colonialisationStartHeight)
        {
            Vector3 branch = growingBranches.Dequeue();

            Vector3 newBranch = GrowTrunk(branch);

            branches.Add(Tuple.Create(branch, newBranch));

            growingBranches.Enqueue(newBranch);

            failsafe++;

            if (failsafe > 999)
            {
                Debug.Log("Failsafe");
            }
        }

        while (growingBranches.Count > 0 && failsafe < 1000)
        {
            Vector3 branch = growingBranches.Dequeue();

            Vector3 newBranch = GrowBranch(branch, points);

            branches.Add(Tuple.Create(branch, newBranch));

            if (CanGrow(newBranch, points))
            {
                growingBranches.Enqueue(newBranch);

                for (int i = 0; i < points.Count; i++)
                {
                    if (Vector3.Distance(newBranch, points[i]) < branchLength)
                    {
                        points.Remove(points[i]);
                    }
                }
            }

            failsafe++;

            if(failsafe > 999)
            {
                Debug.Log("Failsafe");
            }
        }

        return branches;
    }

    private Vector3 GrowTrunk(Vector3 branch)
    {
        return new Vector3(branch.x, branch.y + 1f, branch.z);
    }

    private Vector3 GrowBranch(Vector3 branch, List<Vector3> points)
    {
        //  Dont work for shit
        
        List<Vector3> closePoints = new List<Vector3>();

        foreach(Vector3 point in points)
        {
            if(Vector3.Distance(branch, point) < branchGrowRangeToPoints)
            {
                closePoints.Add(point);
            }
        }

        Vector3 direction = Vector3.zero;

        foreach(Vector3 point in closePoints)
        {
            direction += (point - branch).normalized;
        }

        direction = Vector3.Normalize(direction);

        return branch + (direction * branchLength);
    }

    private bool CanGrow(Vector3 branch, List<Vector3> points)
    {
        if(branch.y > colonialisationStartHeight + colonialisationHeight)
        {
            return false;
        }

        if(-colonialisationRadius > branch.x && branch.x > colonialisationRadius)
        {
            return false;
        }

        foreach(Vector3 point in points)
        {
            if(Vector3.Distance(branch, point) <= branchGrowRangeToPoints)
            {
                return true;
            }
        }

        return false;
    }



    private List<Vector3> GetPoints(Vector2 pos)
    {
        List<Vector3> points = new List<Vector3>();

        for(int i = 0; i < colonialisationPointAmount; i++)
        {
            points.Add(GetPoint(pos, i));
        }

        return points;
    }

    private Vector3 GetPoint(Vector2 pos, float index)
    {
        float x = GetRandomValue(pos, index, colonialisationRadius * 2f);
        float y = GetRandomValue(pos, index + 3, colonialisationHeight) + colonialisationStartHeight;
        float z = GetRandomValue(pos, index + 7, colonialisationRadius * 2f);

        return new Vector3(x, y, z);
    }

    public float GetRandomValue(Vector2 pos, float index, float range)
    {
        //  First grabs some perlin to act as the noise
        //  Then does some mathematical maths on with that perlin,
        //  So that small changes in position, equal large changes in numbers
        //  Then gets the modulo of the value, as to limit return value from 0 to modulo

        float perlin = Mathf.PerlinNoise(((pos.x + (index * 10f)) * 1111f) + offset + 0.1f, ((pos.y + (index * 10f)) * 1111f) + offset + 0.1f);

        float value = (pos.x * 1111f) + (pos.y * 1111f) + perlin;
        value *= perlin;
        value *= value;

        value %= range;

        return value;
    }
}