using System.Collections.Generic;
using UnityEngine;

public class TownData
{
    public Town town;

    public Vector3 townPosition;
    public Vector2 townPosition2;

    public int sizeRadius;
    public float density;

    public List<Vector2> structureNodes = new List<Vector2>();

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

        world.modifications.Enqueue(queue);
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

        Queue<VoxelModification> tempQueue = new Queue<VoxelModification>();
        tempQueue.Enqueue(new VoxelModification(roadPosition, blockID));

        world.modifications.Enqueue(tempQueue);
    }
}
