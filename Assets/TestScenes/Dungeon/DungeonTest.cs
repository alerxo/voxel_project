using UnityEngine;

public class DungeonTest : MonoBehaviour
{
    public Dungeon testDungeon;

    private void Start()
    {
        World world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();

        Debug.Log("Generating dungeon");

        Vector3 dungeonPosition = world.chunksInWorld[World_Settings.worldSizeInChunks / 2, World_Settings.worldSizeInChunks / 2].position;
        dungeonPosition.y = TerrainHeight.GetTerrainHeight(world, new Vector2(dungeonPosition.x, dungeonPosition.z));

        Vector3 dungeonEntrancePosition = dungeonPosition;

        Debug.Log(dungeonPosition.y);

        DungeonData dungeonData = new DungeonData(world, testDungeon, dungeonPosition, dungeonEntrancePosition, 0);
        GenerateDungeon.GenerateDungeonData(dungeonData);

        Debug.Log("Done");
    }
}
