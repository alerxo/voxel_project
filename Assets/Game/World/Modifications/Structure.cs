using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Structure", menuName = "StructureMenu/Structure")]
public class Structure : ScriptableObject
{
    public VoxelModification[] voxels;

    public float scale;
    public float threshold;
    public float offset;

    public bool isSurfaceStructure;
}
