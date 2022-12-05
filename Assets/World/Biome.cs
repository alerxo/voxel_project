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

    public Lode[] lodes;
}
