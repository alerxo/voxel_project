using UnityEngine;

[CreateAssetMenu(fileName = "Dungeon", menuName = "Modifications/Dungeon")]
public class Dungeon : ScriptableObject
{
    public string dungeonName;

    public int dungeonSize;
    public int dungeonSizeVariation;

    public float dungeonDensity;
    public float dungeonDensityVariation;

    public Structure[] rooms;

    public int amountOfDungeons;
}
