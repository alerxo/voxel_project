using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SubBiome", menuName = "TerrainMenu/SubBiome")]
public class SubBiome : ScriptableObject
{
    public string subBiomeName;

    public float threshold;

    [Space]

    [Tooltip("Additional height")]
    public int height;

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
    [Tooltip("Noise can not get higher than this")]
    public float noiseCutoff;

    [Space]
    [Tooltip("How much distance affects blend value")]
    public float blendDistance;
    [Tooltip("How much noise is added to blend value")]
    public float blendNoiseImpact;

    [Space]

    [Header("Blocks")]
    public byte surfaceBlockId;
    public byte subSurfaceBlockId;

    [Header("Lodes")]

    public Lode[] lodes;

    [Header("Structures")]

    public Structure[] structures;

    [Header("TreeStructures")]

    public TreeStructure[] treeStructures;
}
