using UnityEngine;

[CreateAssetMenu(fileName = "Town", menuName = "Modifications/Town")]
public class Town : ScriptableObject
{
    public string townName;

    [Space]

    public int townSize;
    public int townSizeVariation;

    [Space]

    public float townDensity;
    public float townDensityVariation;

    [Space]

    public Structure[] townCenter;
    public Structure[] houses;
    public byte[] roadIds;

    [Space]

    public int amountOfTowns;
}
