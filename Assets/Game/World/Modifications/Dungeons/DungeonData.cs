using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;
using UnityEngine;

public class DungeonData
{
    public Dungeon dungeon;

    public Vector3 dungeonPosition;
    public Vector2 dungeonPosition2;

    public int levels;
    public float density;
    public int sizeRadius;

    public int[] levelYPosition;

    public List<Vector2> roomNodes = new List<Vector2>();

    public List<ChunkCoordinate> hallwayChunks = new List<ChunkCoordinate>();
    public List<Queue<VoxelModification>> hallwayQueues = new List<Queue<VoxelModification>>();

    private World world;

    public DungeonData(World _world, Dungeon _dungeon, Vector3 _position)
    {
        world = _world;
        dungeon = _dungeon;
        dungeonPosition = _position;
        dungeonPosition2 = new Vector2(_position.x, _position.z);

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

        return Mathf.FloorToInt(value);
    }

    public void AddStructure(Structure structure, Vector3 position)
    {
        //  Updates postion with terrainHeight
        //  Adds the structure to the world modifications

        Queue<VoxelModification> queue = new Queue<VoxelModification>();

        roomNodes.Add(new Vector2(position.x, position.z));

        foreach (VoxelModification vm in structure.GetVoxels())
        {
            queue.Enqueue(new VoxelModification(vm.position + position, vm.blockId));
        }

        world.EnqueueModification(queue);
    }

    public void AddHallway(byte blockID, Vector2 position, int yPosition)
    {
        //  Updates postion with terrainHeight
        //  Adds the structure to the world modifications

        Vector3 hallwayPosition = new Vector3(position.x, 1, position.y) + dungeonPosition;

        hallwayPosition.y = yPosition;

        ChunkCoordinate cc = Voxel_Util.GetChunkCoordinateFromVector3(hallwayPosition);
        VoxelModification vm = new VoxelModification(hallwayPosition, blockID);

        int chunkExistsIndex = ChunkExists(cc);

        if (chunkExistsIndex >= 0)
        {
            hallwayQueues[chunkExistsIndex].Enqueue(vm);
        }

        else
        {
            hallwayChunks.Add(cc);
            hallwayQueues.Add(new Queue<VoxelModification>());

            hallwayQueues.Last().Enqueue(vm);
        }
    }

    private int ChunkExists(ChunkCoordinate path)
    {
        for (int i = 0; i < hallwayChunks.Count; i++)
        {
            if (hallwayChunks[i].Equals(path))
            {
                return i;
            }
        }

        return -1;
    }

    public void CompleteHallway()
    {
        foreach (Queue<VoxelModification> queue in hallwayQueues)
        {
            world.EnqueueModification(queue);
        }
    }
}
