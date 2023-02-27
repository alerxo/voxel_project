using UnityEngine;

public class DungeonTest : MonoBehaviour
{
    public Dungeon testDungeon;

    private void Start()
    {
        World world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();

        Debug.Log("Generating dungeon");

        Vector3 dungeonPosition = world.chunksInWorld[World_Settings.worldSizeInChunks / 2, World_Settings.worldSizeInChunks / 2].position;

        DungeonData dungeonData = new DungeonData(world, testDungeon, dungeonPosition);
        GenerateDungeon.GenerateDungeonData(dungeonData);

        Debug.Log("Done");
    }
}
