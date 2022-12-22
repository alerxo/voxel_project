using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome", menuName = "TerrainMenu/Biome")]
public class Biome : ScriptableObject
{
    public string biomeName;

    [Tooltip("Max -and min terrainheight should always be between 1 and chunkheight, also max shouldnt be lower than min")]
    public int maxTerrainHeight;
    [Tooltip("Max -and min terrainheight should always be between 1 and chunkheight, also max shouldnt be lower than min")]
    public int minTerrainHeight;

    [Space]

    [Header("Noise values\nfreqency = x, less means smaller detail\namplitude = y, more means steeper curves")]

    [Space]

    [Tooltip("Distance between samplepoints, bigger lumps")]
    public float noiseScale;
    [Tooltip("Each octave adds another layer of noise")]
    public int octaves;
    [Tooltip("Controls decrease in amplitude of octaves, how much the small features made from lacunarity influence the curve")]
    [Range(0f, 1f)]
    public float persistence;
    [Tooltip("Controls increase in frequence of octaves, small features on the curve")]
    public float lacunarity;

    [Space]

    [Header("Blocks")]
    public byte surfaceBlockId;
    public byte subSurfaceBlockId;

    [Header("Lodes")]

    public Lode[] lodes;

    private void OnValidate()
    {
        //  max and min terrainheight should always be between 1 and chunkheight, also max shouldnt be lower than min

        if(maxTerrainHeight < minTerrainHeight) maxTerrainHeight = minTerrainHeight;

        if (maxTerrainHeight >= VoxelData.chunkHeight) maxTerrainHeight = VoxelData.chunkHeight;
        if (maxTerrainHeight < 1) maxTerrainHeight = 1;

        if (minTerrainHeight >= VoxelData.chunkHeight) minTerrainHeight = VoxelData.chunkHeight;
        if (minTerrainHeight < 1) minTerrainHeight = 1;

        //  Lacunarity should always be larger than 1

        if (lacunarity < 1) lacunarity = 1;

        //  We cant have negative octaves

        if (octaves < 0) octaves = 0;
    }
}
