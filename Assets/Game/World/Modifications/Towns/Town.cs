using UnityEngine;

[CreateAssetMenu(fileName = "Town", menuName = "Modifications/Town")]
public class Town : ScriptableObject
{
    public string townName;

    public int townSize;
    public int townSizeVariation;

    public float townDensity;
    public float townDensityVariation;

    public Structure[] townCenter;
    public Structure[] houses;
    public byte[] roadIds;

    public int amountOfTowns;
}
