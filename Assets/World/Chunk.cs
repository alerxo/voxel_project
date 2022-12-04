using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> verticies = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    bool[,,] voxelMap = new bool[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    private void Start()
    {
        PopulateVoxelMap();

        CreateMeshData();

        CreateMesh();
    }

    private void PopulateVoxelMap()
    {
        //  Fills the chunk with voxels
        //  (not creating mesh, just saying they are there)

        for (int y = 0; y < VoxelData.chunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.chunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    voxelMap[x, y, z] = true;
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
        //  Returns true if voxel is there

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1) 
            return false;

        return voxelMap[x, y, z];
    }

    private void AddVoxelToChunk(Vector3 pos)
    {
        //  Goes through each face of voxel
        //  Checking if there is something adjacent
        //  If not, create verticies for face

        for (int r = 0; r < 6; r++) // Each row (face)
        {
            if(!CheckVoxel(pos + VoxelData.faceChecks[r])) 
            {
                for (int c = 0; c < 6; c++) // Each column (verticie)
                {
                    //  Adding Verticies

                    int triangleIndex = VoxelData.voxelTriangles[r, c];
                    verticies.Add(VoxelData.voxelVerticies[triangleIndex] + pos);

                    //  Adding the triangles in order

                    triangles.Add(vertexIndex);
                    vertexIndex++;

                    //  Setting the uvs with the column index

                    uvs.Add(VoxelData.voxelUvs[c]);
                }
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
}
