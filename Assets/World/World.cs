using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class World : MonoBehaviour
{
    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Block[] blocks;

    private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    private List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate> ();

    ChunkCoordinate playerChunk;
    ChunkCoordinate playerLastChunk;

    private void Start()
    {
        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight + 2f, (VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f);

        GenerateWorld();

        playerLastChunk = GetChunkCoordinateFromVector3 (player.position);
        playerChunk = GetChunkCoordinateFromVector3(player.position);
    }

    private void Update()
    {
        playerChunk = GetChunkCoordinateFromVector3(player.position);

        if((playerChunk.x + playerChunk.z) > (playerLastChunk.x + playerLastChunk.z + 2) || (playerChunk.x + playerChunk.z) < (playerLastChunk.x + playerLastChunk.z - 2))
            CheckRenderDistance();
    }

    private void GenerateWorld()
    {
        //  Generates the world according to world size in chunks
        //  Moves player to spawn

        for(int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.RenderDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.RenderDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.RenderDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.RenderDistanceInChunks; z++)
            {
                CreateNewChunk(x, z);
            }
        }

        player.position = spawnPosition;
    }

    private ChunkCoordinate GetChunkCoordinateFromVector3(Vector3 pos)
    {
        //  Gets the chunk coordinate from a position

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return new ChunkCoordinate(x, z);
    }

    private void CheckRenderDistance()
    {
        //  Checks if there are any chunks in render distance that should exist but dont
        //  If so, generate
        //  Else if it exists but not active, activate
        //  Then check if any old chunks are outside of render distance
        //  If so, deactivate

        ChunkCoordinate coord = GetChunkCoordinateFromVector3(player.position);

        List<ChunkCoordinate> previoslyActiveChunks = new List<ChunkCoordinate>(activeChunks);

        for (int x = coord.x - VoxelData.RenderDistanceInChunks; x < coord.x + VoxelData.RenderDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.RenderDistanceInChunks; z < coord.z + VoxelData.RenderDistanceInChunks; z++)
            {
                if (IsChunkInWorld(new ChunkCoordinate(x, z)))
                {
                    if (chunks[x, z] == null)
                    {
                        CreateNewChunk(x, z);
                    }

                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                        activeChunks.Add(new ChunkCoordinate(x, z));
                    }
                }

                for (int i = 0; i < previoslyActiveChunks.Count; i++)
                {
                    if (previoslyActiveChunks[i].Equals(new ChunkCoordinate(x, z)))
                        previoslyActiveChunks.RemoveAt(i);
                    
                }
            }
        }

        foreach (ChunkCoordinate c in previoslyActiveChunks)
        {
            activeChunks.Remove(new ChunkCoordinate(c.x, c.z));
            chunks[c.x, c.z].isActive = false;
        }
            
    }

    public byte GetVoxel(Vector3 pos)
    {
        //  Basic block assignment

        if (!IsVoxelInWorld(pos))
        {
            return 0;
        }

        if (pos.y == VoxelData.chunkHeight - 1)
        {
            return 1;
        }
        else if (pos.y < VoxelData.chunkHeight && pos.y > VoxelData.chunkHeight - 4)
        {
            return 3;
        }
        else
        {
            return 2;
        }
    }

    private void CreateNewChunk(int x, int z)
    {
        //  Creates a chunk at coord

        chunks[x, z] = new Chunk(new ChunkCoordinate(x, z), this);

        activeChunks.Add(new ChunkCoordinate(x, z));
    }

    private bool IsChunkInWorld(ChunkCoordinate coord)
    {
        //  Checks if chunk is inside world

        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks)
            return true;
        else
            return false;
    }

    private bool IsVoxelInWorld(Vector3 pos)
    {
        //  Checks if voxel is inside world

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxel && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxel)
            return true;
        else
            return false;
    }
}