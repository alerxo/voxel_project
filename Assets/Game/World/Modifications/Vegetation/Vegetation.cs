using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Structure", menuName = "Modifications/Vegetation")]
public class Vegetation : ScriptableObject
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
    public float branchKillRange;

    [Space]

    public float branchRadius;
    public int leafRadius;

    public float branchParentFalloff;

    [Space]

    public float rootAmount;
    public float rootRadius;
    public float rootDepth;
    public float rootHeight;
    public int rootSlimness;

    [Space]

    public float trunkLean;

    private List<Vector3> points = new List<Vector3>();


    public VoxelModification[] GetVoxels(Vector3 pos)
    {
        //  Creates an array of voxels resemblening a tree
        //  Using an implenemtation of space colonialisation

        //  First gets all of the branches, ie the wooden part, trunk, root etc
        //  Then goes through all voxels that can possibly be in range of these branches
        //  Then checks each all branches
        //  Checks if they are close enough to be a wood,
        //  If not, checks if they are close enough to be a leaf
        //  If not its air

        List<VoxelModification> voxels = new List<VoxelModification>();
        List<Tuple<Vector3, Vector3, int>> branches = GetBranches(new Vector2(pos.x, pos.z));

        for(int x = Mathf.FloorToInt(-colonialisationRadius); x < colonialisationRadius; x++)
        {
            for (int y = Mathf.FloorToInt(-rootDepth); y < colonialisationHeight + colonialisationStartHeight; y++)
            {
                for (int z = Mathf.FloorToInt(-colonialisationRadius); z < colonialisationRadius; z++)
                {
                    //  Getting the distance from center of tree, cus the further away, the fewer leaves and branches

                    Vector3 voxelPos = new Vector3(x, y, z);
                    Vector3 centerPos = new Vector3(0, colonialisationStartHeight, 0);

                    float maxDistance = Vector3.Distance(centerPos, new Vector3(0, colonialisationStartHeight + colonialisationHeight, 0));
                    float distanceToCenter = 1f - (Vector3.Distance(voxelPos, centerPos) / maxDistance);

                    distanceToCenter = Mathf.Clamp(distanceToCenter, 0.2f, 1f);

                    //  The branch checks

                    foreach (Tuple<Vector3, Vector3, int> branch in branches)
                    {
                        float distance = DistanceToBranch(branch, voxelPos);

                        if (branch.Item1 != branch.Item2 && distance < (branchRadius - (branch.Item3 * (branchParentFalloff / (distanceToCenter + 0.1f)))))
                        {
                            voxels.Add(new VoxelModification(voxelPos, woodId));
                        }

                        else if(branch.Item1 == branch.Item2 && Vector3.Distance(voxelPos, branch.Item1) < leafRadius * distanceToCenter)
                        {
                            voxels.Add(new VoxelModification(voxelPos, leafId));
                        }
                    }
                }
            }
        }

        points = null;

        return voxels.ToArray();
    }

    private float DistanceToBranch(Tuple<Vector3, Vector3, int> branch, Vector3 voxelPos)
    {
        //  Creates a line between the branch start and branch end
        //  Gets the distance from the voxels position to this line
        //  Clamps it between 0 and 1, and returns

        //  Wikipedia maths "Distance from a point to a line"

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
        //  First getes all the points used in the colinialisation
        //  Branches holds 3 values, start pos, end pos, and number of parents, parents used for branch width
        //  Then creates the root branch at the rootheight
        
        //  Creates all the roots, trunk, and branches

        points = GetPoints(pos2d);

        List<Tuple<Vector3, Vector3, int>> branches = new List<Tuple<Vector3, Vector3, int>>();
        Queue<Tuple<Vector3, int>> growingBranches = new Queue<Tuple<Vector3, int>>();

        growingBranches.Enqueue(Tuple.Create(new Vector3(0, rootHeight, 0), 0));

        int failsafe = 0; //    Just in case so we dont do infinite loop

        for(int i = 0; i < rootAmount; i++)
        {
            //  Gets random start pos for the root, and sets end at trunk start

            branches.Add(Tuple.Create(GrowRoot(pos2d, i), new Vector3(0, rootHeight, 0), rootSlimness));
        }

        //  Gets the lean direction of trunk

        float xLean = GetRandomValue(pos2d, 4, trunkLean * 2f) - trunkLean;
        float zLean = GetRandomValue(pos2d, 4, trunkLean * 2f) - trunkLean;

        for (int i = (int)rootHeight; i < colonialisationStartHeight; i++)
        {
            //  Creates all the trunk branches, final one has a parent, as to get leaves

            Tuple<Vector3, int> branch = growingBranches.Dequeue();

            Vector3 start = branch.Item1;
            Vector3 end = GrowTrunk(start, xLean, zLean);

            growingBranches.Enqueue(Tuple.Create(end, 0));

            if (i >= colonialisationStartHeight - 1)
            {
                branches.Add(Tuple.Create(start, end, 1));
            }

            else
            {
                branches.Add(Tuple.Create(start, end, 0));
            }
        }

        while (growingBranches.Count > 0 && failsafe < 1000)
        {
            //  Creates a new branch, start pos is parent end pos, end pos is grabbed by colonialisation
            //  If end isnt vector zero, ie, it has grown, add it to branch list
            //  Then checks if parent branch can continue growing, if it can readd it to the growing branches queue
            //  Same if the newly created branch

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

    private Vector3 GrowRoot(Vector2 pos, int index)
    {
        //  Gets random values within root radius and below the surface

        float x = GetRandomValue(pos, index, rootRadius * 2f) - rootRadius;
        float y = GetRandomValue(pos, index + 3, rootDepth) - rootDepth;
        float z = GetRandomValue(pos, index + 7, rootRadius * 2f) - rootRadius;

        return new Vector3(x, y, z);
    }

    private Vector3 GrowTrunk(Vector3 branch, float xLean, float zLean)
    {
        //  Grows upwards by one unit and adds lean

        return new Vector3(branch.x + xLean, branch.y + 1f, branch.z + zLean);
    }

    private Vector3 GrowBranch(Vector3 branch, List<Vector3> points)
    {
        //  Space colonialisation
        //  Gets all of the points in range,
        //  adds the vector3s together, then gets the average, this is the branch end position
        //  Then removes the points within kill range of branch end pos
        
        List<Vector3> closePoints = new List<Vector3>();

        for (int i = 0; i < points.Count; i++)
        {
            if(Vector3.Distance(branch, points[i]) < branchGrowRangeToPoints)
            {
                closePoints.Add(points[i]);
            }
        }

        Vector3 position = Vector3.zero;

        foreach (Vector3 point in closePoints)
        {
            position += point;
        }

        if(closePoints.Count > 1)
        {
            position /= closePoints.Count;
        }

        for (int i = 0; i < points.Count; i++)
        {
            if (Vector3.Distance(position, points[i]) < branchKillRange)
            {
                points.Remove(points[i]);
            }
        }

        points.TrimExcess();

        return position;
    }

    private bool CanGrow(Vector3 branch, int parents, List<Vector3> points)
    {
        //  Can grow if branch radius exists, is withing colonialisation dimensions, and has points in range

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
        //  Gets random points, ie vector3s, within colonisation dimension

        List<Vector3> points = new List<Vector3>();

        for(int i = 0; i < colonialisationPointAmount; i++)
        {
            points.Add(GetPoint(pos, i));
        }

        return points;
    }

    private Vector3 GetPoint(Vector2 pos, float index)
    {
        //  Pseudorandom values

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