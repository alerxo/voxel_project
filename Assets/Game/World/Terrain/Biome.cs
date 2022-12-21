using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome", menuName = "TerrainMenu/Biome")]
public class Biome : ScriptableObject
{
    public string biomeName;

    public int maxGroundY;
    public int minGroundY;

    public float terrainNoiseScale;

    [Header("Blocks")]
    public byte surfaceBlockId;
    public byte subSurfaceBlockId;

    [Header("Lodes")]

    public Lode[] lodes;
}
