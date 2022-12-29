using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Climate", menuName = "TerrainMenu/Climate")]
public class Climate : ScriptableObject
{
    public string climateName;

    public Biome[] biomes;

    public int minClimateSizeInChunks;
}
