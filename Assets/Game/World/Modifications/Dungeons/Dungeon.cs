using UnityEngine;

[CreateAssetMenu(fileName = "Dungeon", menuName = "Modifications/Dungeon")]
public class Dungeon : ScriptableObject
{
    public string dungeonName;

    [Space]

    public int size;
    public int sizeVariation;

    [Space]

    public float roomDensity;
    public float densityVariation;

    [Space]

    public int amountOfLevels;
    public int levelVariation;

    [Space]

    public Structure[] entrances;
    public Structure[] rooms;
    public Structure[] stairs;

    [Space]

    public int amountOfDungeons;
}
