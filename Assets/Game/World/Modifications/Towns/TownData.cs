using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TownData
{
    public Town town;

    public Vector3 townPosition;
    public Vector2 townPosition2;

    public int sizeRadius;
    public float density;

    public List<Vector2> structureNodes = new List<Vector2>();

    public List<ChunkCoordinate> roadChunks = new List<ChunkCoordinate>();
    public List<Queue<VoxelModification>> roadQueues = new List<Queue<VoxelModification>>();

    private World world;

    public TownData(World _world, Town _town, Vector3 _position)
    {
        world = _world;
        town = _town;

        townPosition = _position;
        townPosition2 = new Vector2(townPosition.x, townPosition.z);
    }
    
    public void AddStructure(Structure structure, Vector2 position)
    {
        //  Updates postion with terrainHeight
        //  Adds the structure to the world modifications

        structureNodes.Add(position);

        Queue<VoxelModification> queue = new Queue<VoxelModification>();

        Vector3 structurePosition = new Vector3(position.x, 1, position.y);
        SubBiome structureChunkBiome = Voxel_Util.GetChunkFromVector3(world, structurePosition).subBiome;

        if (structureChunkBiome.parentBiome == null) return;

        structurePosition.y = TerrainHeight.GetTerrainHeight(world, new Vector2(structurePosition.x, structurePosition.z));

        foreach (VoxelModification vm in structure.GetVoxels())
        {
            queue.Enqueue(new VoxelModification(vm.position + structurePosition, vm.blockId));
        }

        world.EnqueueModification(queue);
    }

    public void AddRoad(byte blockID, Vector2 position)
    {
        //  Updates postion with terrainHeight
        //  Adds the structure to the world modifications

        Vector3 roadPosition = new Vector3(position.x, 1, position.y) + townPosition;

        SubBiome structureChunkBiome = Voxel_Util.GetChunkFromVector3(world, roadPosition).subBiome;

        if (structureChunkBiome == null)
        {
            Debug.LogError("structureChunkBiome is null");
            return;
        }
            
        roadPosition.y = TerrainHeight.GetTerrainHeight(world, new Vector2(roadPosition.x, roadPosition.z));

        ChunkCoordinate cc = Voxel_Util.GetChunkCoordinateFromVector3(roadPosition);
        VoxelModification vm = new VoxelModification(roadPosition, blockID);

        int chunkExistsIndex = ChunkExists(cc);

        if (chunkExistsIndex >= 0)
        {
            roadQueues[chunkExistsIndex].Enqueue(vm);
        }

        else
        {
            roadChunks.Add(cc);
            roadQueues.Add(new Queue<VoxelModification>());

            roadQueues.Last().Enqueue(vm);
        }
    }

    private int ChunkExists(ChunkCoordinate path)
    {
        for (int i = 0; i < roadChunks.Count; i++)
        {
            if (roadChunks[i].Equals(path))
            {
                return i;
            }
        }

        return -1;
    }

    public void CompleteHallway()
    {
        foreach (Queue<VoxelModification> queue in roadQueues)
        {
            world.EnqueueModification(queue);
        }
    }
}
