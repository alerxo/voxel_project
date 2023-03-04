using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonData
{
    public Dungeon dungeon;

    public Vector3 position3D;
    public Vector2 position2D;

    public Vector3 entrancePosition;

    public int levels;
    public float density;
    public int size;

    public int nextYPosition;

    public List<Vector2> roomNodes = new List<Vector2>();
    public List<Vector2> cachedRoomNodes = new List<Vector2>();

    public List<ChunkCoordinate> hallwayChunks = new List<ChunkCoordinate>();
    public List<Queue<VoxelModification>> hallwayQueues = new List<Queue<VoxelModification>>();

    private World world;

    public DungeonData(World _world, Dungeon _dungeon, Vector3 _position, Vector3 _entrancePosition, int _entranceLevel)
    {
        world = _world;
        dungeon = _dungeon;

        position3D = _position;
        position2D = new Vector2(_position.x, _position.z);
        entrancePosition = _entrancePosition;

        GetData();
    }

    private void GetData()
    {
        size = dungeon.size + (dungeon.sizeVariation - (Structure_Util.GetRandomValue(position2D, 0, dungeon.sizeVariation * 2)));

        levels = dungeon.amountOfLevels + (dungeon.levelVariation - (Structure_Util.GetRandomValue(position2D, 0, dungeon.levelVariation * 2)));

        density = dungeon.roomDensity + (dungeon.densityVariation - ((float)Structure_Util.GetRandomValue(position2D, 0, (Mathf.FloorToInt(dungeon.densityVariation * 100)) * 2) / 100f));
    }

    public void AddStructure(Structure structure, Vector3 position)
    {
        //  Adds the structure to the world modifications

        if (!Structure_Util.CheckIfOverlap(structure, position, true))
        {
            Queue<VoxelModification> queue = new Queue<VoxelModification>();

            roomNodes.Add(new Vector2(position.x, position.z));

            foreach (VoxelModification vm in structure.GetVoxels())
            {
                queue.Enqueue(new VoxelModification(vm.position + position, vm.blockId));
            }

            world.EnqueueModification(queue);
        }
    }

    public void AddStair(Structure structure, Vector3 position)
    {
        //  Adds the structure to the world modifications

        if (!Structure_Util.CheckIfOverlap(structure, position, true))
        {
            Queue<VoxelModification> queue = new Queue<VoxelModification>();

            roomNodes.Add(new Vector2(position.x, position.z));

            foreach (VoxelModification vm in structure.GetVoxels())
            {
                queue.Enqueue(new VoxelModification(vm.position + position, vm.blockId));
            }

            world.EnqueueModification(queue);
        }
    }

    public void AddHallway(byte blockID, Vector2 position, int yPosition)
    {
        //  Updates postion with terrainHeight
        //  Adds the structure to the world modifications

        Vector3 hallwayPosition = new Vector3(position.x, 1, position.y) + position3D;

        hallwayPosition.y = yPosition + 1;

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
