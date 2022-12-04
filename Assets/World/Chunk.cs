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

    private void Start()
    {
        AddVoxelToChunk(transform.position);
        CreateMesh();
    }

    private void AddVoxelToChunk(Vector3 pos)
    {
        for (int r = 0; r < 6; r++)
        {
            for (int c = 0; c < 6; c++)
            {
                //  Following the order of rows and columns in VoxelData.voxelTriangles
                //  and making a list of verticies in the right order

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
