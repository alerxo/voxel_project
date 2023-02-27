using UnityEngine;

[CreateAssetMenu(fileName = "Dungeon", menuName = "Modifications/Dungeon")]
public class Dungeon : ScriptableObject
{
    public string dungeonName;

    [Space]

    public int dungeonSize;
    public int dungeonSizeVariation;

    [Space]

    public float dungeonDensity;
    public float dungeonDensityVariation;

    [Space]

    public int dungeonLevelAmount;
    public int dungeonLevelAmountVariation;

    [Space]

    public int dungeonLevelGap;

    [Space]

    public Structure[] rooms;

    [Space]

    public int lowestY;
    public int highestY;

    [Space]

    public int amountOfDungeons;
}
