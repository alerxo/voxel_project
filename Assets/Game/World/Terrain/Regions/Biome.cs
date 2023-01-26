using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome", menuName = "TerrainMenu/Biome")]
public class Biome : ScriptableObject
{
    public string biomeName;

    [Space]

    public float maxTemperature;
    public float minTemperature;

    [Space]

    public float maxHumidity;
    public float minHumidity;

    [Space]

    public int maxHeight;
    public int minHeight;

    [Space]

    public int arrayPosition;

    public SubBiome[] subBiomes;
}
