using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData
{
    //  World settings

    public static readonly int chunkWidth = 8;
    public static readonly int chunkHeight = 128;

    public static readonly int WorldSizeInChunks = 16;

    public static int WorldSizeInVoxel
    {
        get { return WorldSizeInChunks * chunkWidth; }
    }

    // Player settings

    public static readonly int RenderDistanceInChunks = 4;

    //  Texture atlas

    public static readonly int TextureAtlasBlockWidth = 4;
    public static float NormalizedBlockTextureSize
    {
        get { return 1f / (float)TextureAtlasBlockWidth; }
    }

    public static readonly Vector3[] voxelVerticies = new Vector3[8]
    {
        //  The 8 corners of a voxel (cube)

        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),
    };

    public static readonly Vector3[] faceChecks = new Vector3[6]
    {
        //  Table of which direction to check for adjacent voxel for each face

        new Vector3(0.0f, 0.0f, -1.0f), // Back face
        new Vector3(0.0f, 0.0f, 1.0f), // Front face
        new Vector3(0.0f, 1.0f, 0.0f), // Top face
        new Vector3(0.0f, -1.0f, 0.0f), // Bottom face
        new Vector3(-1.0f, 0.0f, 0.0f), // Left face
        new Vector3(1.0f, 0.0f, 0.0f) // Right face
    };

    public static readonly int[,] voxelTriangles = new int[6, 4]
    {
        //  Describes the order in which we take verticies from "voxelVerticies" and build triangles from

        {0, 3, 1, 2}, // Back face
        {5, 6, 4, 7}, // Front face
        {3, 7, 2, 6}, // Top face
        {1, 5, 0, 4}, // Bottom face
        {4, 7, 0, 3}, // Left face
        {1, 2, 5, 6} // Right face
    };

    public static readonly Vector2[] voxelUvs = new Vector2[4]
    {
        //  Sets the two triangles for uvs at the corners

        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f)
    };
}
