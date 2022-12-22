using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Climate", menuName = "TerrainMenu/Climate")]
public class Climate : ScriptableObject
{
    public string biomeName;

    public Biome[] biomes;
    public float[] thresholds;

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
}
