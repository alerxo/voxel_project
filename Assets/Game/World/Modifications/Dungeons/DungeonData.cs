using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class DungeonData
{
    public Dungeon dungeon;

    public Vector3 dungeonPosition;

    public int levels;
    public float density;
    public int sizeRadius;

    public int[] levelYPosition;

    private World world;

    public DungeonData(World _world, Dungeon _dungeon, Vector3 _position)
    {
        world = _world;
        dungeon = _dungeon;
        dungeonPosition = _position;

        dungeonPosition.y = dungeon.highestY - GetYPosition();
    }

    private float GetYPosition()
    {
        //  First grabs some perlin to act as the noise
        //  Then does some mathematical maths on with that perlin,
        //  So that small changes in position, equal large changes in numbers
        //  Then gets the modulo of the value, to always return a value within range

        int largest = dungeon.highestY - dungeon.lowestY;

        if (largest == 0) return 0;
        float perlin = Mathf.PerlinNoise((dungeonPosition.x * 1111f) * 1111f + 0.1f, (dungeonPosition.z * 1111f) * 1111f + 0.1f);

        float value = (dungeonPosition.x * 1111f) + (dungeonPosition.z * 1111f) + perlin;
        value *= perlin;
        value *= value;

        value %= largest;

        Debug.Log(value);

        return Mathf.FloorToInt(value);
    }

    public void AddStructure(Structure structure, Vector3 position)
    {
        //  Updates postion with terrainHeight
        //  Adds the structure to the world modifications

        Queue<VoxelModification> queue = new Queue<VoxelModification>();

        foreach (VoxelModification vm in structure.GetVoxels())
        {
            queue.Enqueue(new VoxelModification(vm.position + position, vm.blockId));
        }

        world.modifications.Enqueue(queue);
    }
}
