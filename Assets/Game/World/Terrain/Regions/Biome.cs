using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome", menuName = "TerrainMenu/Biome")]
public class Biome : ScriptableObject
{
    public string biomeName;

    public int amount;
    public float weight;
    public float minBiomeSizeInChunks;

    [Tooltip("Additional height")]
    public int height;
    [Tooltip("Lowest possible hieght")]
    public int startHeight;

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

    [Header("Blocks")]
    public byte surfaceBlockId;
    public byte subSurfaceBlockId;

    [Header("Lodes")]

    public Lode[] lodes;
}
