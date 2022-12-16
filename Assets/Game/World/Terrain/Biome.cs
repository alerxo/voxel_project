using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Biome", menuName = "BiomeMenu/Biome")]
public class Biome : ScriptableObject
{
    public string biomeName;

    public int maxGroundY;
    public int minGroundY;

    public float terrainScale;

    [Header("Trees")]
    public float treeZoneScale;
    public float treeZoneThreshHold;

    public float treePlacementScale;
    public float treePlacementThreshold;

    public int minTreeY;
    public int maxTreeY;

    
    [Header("Lodes")]

    public Lode[] lodes;
}
