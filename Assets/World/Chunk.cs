using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    private int vertexIndex = 0;
    private List<Vector3> verticies = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    private byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    private World world;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();

        PopulateVoxelMap();

        CreateMeshData();

        CreateMesh();
    }

    private void PopulateVoxelMap()
    {
        //  Fills the chunk with voxels

        for (int y = 0; y < VoxelData.chunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.chunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    //  Basic block assignment

                    if(y == VoxelData.chunkHeight - 1)
                    {
                        voxelMap[x, y, z] = 0;
                    }
                    else if (y < VoxelData.chunkHeight && y > VoxelData.chunkHeight -4)
                    {
                        voxelMap[x, y, z] = 2;
                    }
                    else
                    {
                        voxelMap[x, y, z] = 1;
                    }
                }
            }
        }
    }

    private void CreateMeshData()
    {
        //  Adding a voxel at each spot in the chunk

        for (int y = 0; y < VoxelData.chunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.chunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    AddVoxelToChunk(new Vector3(x, y, z));
                }
            }
        }
    }

    private bool CheckVoxel(Vector3 pos)
    {
        //  Converts into ints
        //  Checks if voxel pos is inside of chunk
        //  Checks the position of given coordinates for voxel
        //  Returns true if voxel is there and the voxel is a solid block

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1) 
            return false;

        return world.blocks[voxelMap[x, y, z]].isSolid;
    }

    private void AddVoxelToChunk(Vector3 pos)
    {
        //  Goes through each face of voxel
        //  Checking if there is something adjacent
        //  If not, create verticies, uvs and triangles for face

        for (int r = 0; r < 6; r++) // Each row (face)
        {
            if(!CheckVoxel(pos + VoxelData.faceChecks[r])) 
            {
                int blockID = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];

                //  Adding the verticies

                verticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 0]]);
                verticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 1]]);
                verticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 2]]);
                verticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 3]]);

                //  Adding the uvs

                AddTexture(world.blocks[blockID].textureID);

                //  Adding the triangles

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                //  Incrementing the vertex index

                vertexIndex += 4;
            }
        }
    }

    private void CreateMesh()
    {
        //  Adding the info to a mesh and recalulating
        //  and recalculating normals as to make sure all verticies face the right way

        Mesh mesh = new Mesh();
        mesh.vertices = verticies.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    private void AddTexture(int textureID)
    {
        //  Creates x and y coordinates that match the textureID,
        //  as to find it in the texture atlas

        float y = textureID / VoxelData.TextureAtlasBlockWidth;
        float x = textureID - (y * VoxelData.TextureAtlasBlockWidth);   

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize)); 
    }
}
