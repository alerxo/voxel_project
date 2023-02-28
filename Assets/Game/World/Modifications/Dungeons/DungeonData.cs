using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonData
{
    public Dungeon dungeon;

    public Vector3 dungeonPosition;
    public Vector2 dungeonPosition2;
    public int dungeonMinLevel;

    public Vector3 dungeonEntrancePosition;
    public int dungeonEntranceLevel;

    public int levels;
    public float density;
    public int sizeRadius;

    public List<Vector2> roomNodes = new List<Vector2>();

    public List<ChunkCoordinate> hallwayChunks = new List<ChunkCoordinate>();
    public List<Queue<VoxelModification>> hallwayQueues = new List<Queue<VoxelModification>>();

    private World world;

    public DungeonData(World _world, Dungeon _dungeon, Vector3 _position, Vector3 _entrancePosition, int _entranceLevel)
    {
        world = _world;
        dungeon = _dungeon;

        dungeonPosition = _position;
        dungeonPosition2 = new Vector2(_position.x, _position.z);
        dungeonEntrancePosition = _entrancePosition;
        dungeonEntranceLevel = _entranceLevel;

        GetData();
    }

    private void GetData()
    {
        levels = dungeon.dungeonLevelAmount + (dungeon.dungeonLevelAmountVariation - Structure_Util.GetRandomValue(dungeonPosition, 1, dungeon.dungeonLevelAmountVariation * 2));
        sizeRadius = dungeon.dungeonSize + (dungeon.dungeonSizeVariation - Structure_Util.GetRandomValue(dungeonPosition, 1, dungeon.dungeonSizeVariation * 2));
        density = dungeon.dungeonDensity + (dungeon.dungeonDensityVariation - ((float)Structure_Util.GetRandomValue(dungeonPosition, 2, Mathf.FloorToInt(dungeon.dungeonDensityVariation * dungeon.dungeonSize)) / (float)dungeon.dungeonSize));
        dungeonMinLevel = dungeon.dungeonMinLevel + (dungeon.dungeonMinLevelVariation - Structure_Util.GetRandomValue(dungeonPosition, 1, dungeon.dungeonMinLevelVariation * 2));
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
