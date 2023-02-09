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
    public int leafRadius;

    public float branchParentFalloff;

    private List<Vector3> points = new List<Vector3>();
    private int longestBranch = 1;


    public VoxelModification[] GetVoxels(Vector3 pos)
    {
        List<VoxelModification> voxels = new List<VoxelModification>();

        List<Tuple<Vector3, Vector3, int>> branches = GetBranches(new Vector2(pos.x, pos.z));

        for(int x = Mathf.FloorToInt(-colonialisationRadius); x < colonialisationRadius; x++)
        {
            for (int y = 0; y < colonialisationHeight + colonialisationStartHeight; y++)
            {
                for (int z = Mathf.FloorToInt(-colonialisationRadius); z < colonialisationRadius * 2; z++)
                {
                    Vector3 voxelPos = new Vector3(x, y, z);

                    foreach (Tuple<Vector3, Vector3, int> branch in branches)
                    {
                        float distance = DistanceToBranch(branch, voxelPos);

                        if (branch.Item1 != branch.Item2 && distance < branchRadius - (branch.Item3 * branchParentFalloff))
                        {
                            voxels.Add(new VoxelModification(voxelPos, woodId));
                        }

                        else if(branch.Item1 == branch.Item2 && Vector3.Distance(voxelPos, branch.Item1) < leafRadius * 
                            (1f - (Vector3.Distance(voxelPos, new Vector3(0, colonialisationStartHeight + colonialisationHeight / 2, 0)) / ((colonialisationHeight + colonialisationRadius) / 2))))
                        {
                            voxels.Add(new VoxelModification(voxelPos, leafId));
                        }
                    }
                }
            }
        }

        return voxels.ToArray();
    }

    private float DistanceToBranch(Tuple<Vector3, Vector3, int> branch, Vector3 voxelPos)
    {
        //return Vector3.Cross((branch.Item2 - branch.Item1).normalized, voxelPos - branch.Item1).magnitude;

        Vector3 intersection = new Vector3();

        Vector3 start = branch.Item1;
        Vector3 end = branch.Item2;

        float distance = Vector3.Distance(start, end);

        float t = (((voxelPos.x - end.x) * (start.x - end.x)) + ((voxelPos.y - end.y) * (start.y - end.y)) + ((voxelPos.z - end.z) * (start.z - end.z))) / (distance * distance);

        t = Mathf.Clamp01(t);

        intersection.x = end.x + (t * (start.x - end.x));
        intersection.y = end.y + (t * (start.y - end.y));
        intersection.z = end.z + (t * (start.z - end.z));

        return Vector3.Distance(voxelPos, intersection);
    }


    private List<Tuple<Vector3, Vector3, int>> GetBranches(Vector2 pos2d)
    {
        points = GetPoints(pos2d);

        List<Tuple<Vector3, Vector3, int>> branches = new List<Tuple<Vector3, Vector3, int>>();
        
        Queue<Tuple<Vector3, int>> growingBranches = new Queue<Tuple<Vector3, int>>();

        growingBranches.Enqueue(Tuple.Create(new Vector3(0, 0, 0), 0));

        int failsafe = 0;
        bool trunkDone = false;

        while (!trunkDone && failsafe < 1000)
        {
            Tuple<Vector3, int> branch = growingBranches.Dequeue();

            Vector3 start = branch.Item1;

            Vector3 end = GrowTrunk(start);

            growingBranches.Enqueue(Tuple.Create(end, 0));

            if (growingBranches.Peek().Item1.y >= colonialisationStartHeight)
            {
                trunkDone = true;
                branches.Add(Tuple.Create(start, end, 1));
            }

            else
            {
                branches.Add(Tuple.Create(start, end, 0));
            }

       
            failsafe++;

            if (failsafe > 999)
            {
                Debug.Log("Failsafe");
            }
        }

        while (growingBranches.Count > 0 && failsafe < 1000)
        {
            Tuple<Vector3, int> branch = growingBranches.Dequeue();

            Vector3 start = branch.Item1;

            Vector3 end = GrowBranch(start, points);

            if(end != Vector3.zero)
            {
                if (CanGrow(start, branch.Item2, points))
                {
                    branches.Add(Tuple.Create(start, end, branch.Item2 + 1));
                    growingBranches.Enqueue(Tuple.Create(start, branch.Item2 + 1));
                }

                else
                {
                    branches.Add(Tuple.Create(start, start, branch.Item2 + 1));
                }

                if (CanGrow(end, branch.Item2, points))
                {
                    growingBranches.Enqueue(Tuple.Create(end, branch.Item2 + 1));
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

        for (int i = 0; i < points.Count; i++)
        {
            if(Vector3.Distance(branch, points[i]) < branchGrowRangeToPoints)
            {
                closePoints.Add(points[i]);

                if(Vector3.Distance(branch, points[i]) < branchGrowRangeToPoints)
                {
                    points.Remove(points[i]);
                }
            }
        }

        points.TrimExcess();

        Vector3 position = Vector3.zero;

        foreach (Vector3 point in closePoints)
        {
            position += point;
        }

        if(closePoints.Count > 1)
        {
            position /= closePoints.Count;
        }

        return position;
    }

    private bool CanGrow(Vector3 branch, int parents, List<Vector3> points)
    {
        if(branchRadius - (parents * branchParentFalloff) <= 0)
        {
            return false;
        }

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
        float x = GetRandomValue(pos, index, colonialisationRadius * 2f) - colonialisationRadius;
        float y = GetRandomValue(pos, index + 3, colonialisationHeight) + colonialisationStartHeight;
        float z = GetRandomValue(pos, index + 7, colonialisationRadius * 2f) - colonialisationRadius;

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