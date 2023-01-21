using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    #region oof

    public GameObject chunkObject { get; private set; }

    public bool hasBeenEnqueued = false;

    #region lodobj

    public GameObject lodOneObject { get; private set; }
    public GameObject lodOneWaterObject { get; private set; }

    public GameObject lodTwoObject { get; private set; }

    private MeshRenderer lodOneObjectMeshRenderer;
    private MeshFilter lodOneObjectMeshFilter;
    private MeshCollider lodOneObjectMeshCollider;

    private MeshRenderer lodOneWaterObjectMeshRenderer;
    private MeshFilter lodOneWaterObjectMeshFilter;
    private MeshCollider lodOneWaterObjectMeshCollider;

    private MeshRenderer lodTwoObjectMeshRenderer;
    private MeshFilter lodTwoObjectMeshFilter;

    #endregion

    public ChunkCoordinate coord;

    #region lodonemeshdata

    private int lodOneVertexIndex = 0;
    private List<Vector3> lodOneVerticies = new List<Vector3>();

    private List<int> lodOneSolidTriangles = new List<int>();
    private List<int> lodOneTransparentTriangles = new List<int>();

    private List<Vector2> lodOneUvs = new List<Vector2>();

    #endregion

    #region lodonemeshdata

    private int lodOneWaterVertexIndex = 0;
    private List<Vector3> lodOneWaterVerticies = new List<Vector3>();

    private List<int> lodOneWaterTriangles = new List<int>();

    private List<Vector2> lodOneWaterUvs = new List<Vector2>();

    #endregion

    #region lodtwomeshdata

    int lodTwoVertexIndex = 0;

    private List<Vector3> lodTwoVerticies = new List<Vector3>();
    private List<int> lodTwoTriangles = new List<int>();
    private List<Vector2> lodTwoUvs = new List<Vector2>();

    #endregion

    private Material[] materials = new Material[2];

    public Vector3 position;

    public byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    private World world;

    private bool _isActive;
    private bool isVoxelMapPopulated = false;

    // child objects

    public List<GameObject> childObjects = new List<GameObject>();

    //  terrain cache

    public int tempTerrainHeight;

    public float[] bottomRightCornerNoise;
    public float[] topRightCornerNoise;
    public float[] bottomLeftCornerNoise;
    public float[] topLeftCornerNoise;

    public float[] centerNoise;

    #endregion

    public SubBiome biome { get; private set; }

    public Chunk(ChunkCoordinate _coord, World _world)
    {
        coord = _coord;
        world = _world;

        IsActive = false;
    }

    public void Initialize()
    {
        //  Adds an inactove gameobject and sets it to ground layer

        #region gameobjects

        chunkObject = new GameObject();

        lodOneObject = new GameObject();
        lodOneWaterObject = new GameObject();

        lodTwoObject = new GameObject();

        #endregion

        IsActive = world.initializeChunkActive;

        #region layers

        lodOneObject.layer = 8;
        lodOneWaterObject.layer = 4;

        lodTwoObject.layer = 8;

        #endregion

        biome = world.subBiomMap[coord.x, coord.z];

        #region components

        lodOneObjectMeshFilter = lodOneObject.AddComponent<MeshFilter>();
        lodOneObjectMeshRenderer = lodOneObject.AddComponent<MeshRenderer>();
        lodOneObjectMeshCollider = lodOneObject.AddComponent<MeshCollider>();

        lodOneWaterObjectMeshFilter = lodOneWaterObject.AddComponent<MeshFilter>();
        lodOneWaterObjectMeshRenderer = lodOneWaterObject.AddComponent<MeshRenderer>();
        lodOneWaterObjectMeshCollider = lodOneWaterObject.AddComponent<MeshCollider>();

        lodOneWaterObjectMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        lodTwoObjectMeshFilter = lodTwoObject.AddComponent<MeshFilter>();
        lodTwoObjectMeshRenderer = lodTwoObject.AddComponent<MeshRenderer>();

        materials[0] = world.solidMaterial;
        materials[1] = world.transparentMaterial;

        lodOneObjectMeshRenderer.materials = materials;
        lodOneWaterObjectMeshRenderer.material = world.liquidMaterial;
        lodTwoObjectMeshRenderer.materials = materials;

        #endregion

        #region Parents to world obj and changes name

        chunkObject.transform.SetParent(world.transform);

        lodOneObject.transform.SetParent(chunkObject.transform);
        lodOneWaterObject.transform.SetParent(lodOneObject.transform);

        lodTwoObject.transform.SetParent(chunkObject.transform);
        
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.chunkWidth, 0f, coord.z * VoxelData.chunkWidth);

        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
        lodOneObject.name = "Lod one";
        lodOneWaterObject.name = "Lod one water";
        lodTwoObject.name = "Lod two";

        position = chunkObject.transform.position;

        #endregion
    }

    private void UpdateAdjacentVoxels(int x, int y, int z)
    {
        //  Uses face list i voxel data to loop through faces and adds to chunks to update list

        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int r = 0; r < 6; r++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[r];

            if(!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                world.chunksToCreateChunkData.Insert(0, Voxel_Util.GetChunkFromVector3(world, currentVoxel + position));
            }
        }
    }

    public void EditVoxel(Vector3 pos, byte newID)
    {
        //  Changes the ID of voxel and updates itself, and adjacent voxels

        //  Not used rn but can be if block placing should be a thing

        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        voxelMap[xCheck, yCheck, zCheck] = newID;

        lock (world.ThreadedUpdate_Lock)
        {
            world.chunksToCreateChunkData.Insert(0, this);
            UpdateAdjacentVoxels(xCheck, yCheck, zCheck);
        }
    }

    public bool IsActive
    {
        //  If changing the activate statem, if active, lod one, if inactive, lod two, will update child objects, ie entities in chunk,
        //  related to render distance we want to render entities in chunk with chunk

        get { return _isActive; }

        set
        {
            _isActive = value;

            if (chunkObject != null)
            {
                lodOneObject.SetActive(value);
                lodTwoObject.SetActive(!value);

                foreach (GameObject child in childObjects)
                {
                    child.SetActive(value);
                }
            }
        }
    }

    public bool IsEditable
    {
        get
        {
            if (!isVoxelMapPopulated)
                return false;
            else
                return true;
        }
    }

    public void PopulateVoxelMap()
    {
        //  Fills the chunk with voxel data
        //  Locks the chunksupdate list and adds itself, as to avoid confusion between threads

        for (int x = 0; x < VoxelData.chunkWidth; x++)
        {
            for (int z = 0; z < VoxelData.chunkWidth; z++)
            {
                tempTerrainHeight = 0;

                for (int y = 0; y < VoxelData.chunkHeight; y++)
                {
                    voxelMap[x, y, z] = WorldTerrain.GetVoxelBlockType(world, new Vector3(x, y, z) + position, true, tempTerrainHeight);
                }

                tempTerrainHeight = 0;
            }
        }

        isVoxelMapPopulated = true;

        UpdateChunk();
    }

    public void UpdateChunk()
    {
        //  Adds all the modifications to the chunk, ie changing block id
        //  Adding voxel mesh data at each spot in the chunk if its a solid
        //  Adds itself to draw queue, threading -||-

        ClearMeshDataLodOne();
        ClearMeshDataLodTwo();

        UpdateLodOneMeshData();
        UpdateLodTwoMeshData();

        world.chunksToDrawLodOne.Enqueue(this);
        world.chunksToDrawLodTwo.Enqueue(this);
    }

    public void CreateMeshLodOne()
    {
        //  Adding the info to a mesh
        //  recalculating normals as to make sure all verticies face the right way

        Mesh lodOneMesh = new Mesh();
        lodOneMesh.vertices = lodOneVerticies.ToArray();

        lodOneMesh.subMeshCount = 2;

        lodOneMesh.SetTriangles(lodOneSolidTriangles.ToArray(), 0);
        lodOneMesh.SetTriangles(lodOneTransparentTriangles.ToArray(), 1);

        lodOneMesh.uv = lodOneUvs.ToArray();

        lodOneMesh.RecalculateNormals();

        lodOneObjectMeshFilter.mesh = lodOneMesh;
        lodOneObjectMeshCollider.sharedMesh = lodOneObjectMeshFilter.mesh;
    }

    public void CreateMeshWater()
    {
        //  -||-

        Mesh lodOneWaterMesh = new Mesh();
        lodOneWaterMesh.vertices = lodOneWaterVerticies.ToArray();

        lodOneWaterMesh.subMeshCount = 1;
        lodOneWaterMesh.SetTriangles(lodOneWaterTriangles.ToArray(), 0);

        lodOneWaterMesh.uv = lodOneWaterUvs.ToArray();

        lodOneWaterMesh.RecalculateNormals();

        lodOneWaterObjectMeshFilter.mesh = lodOneWaterMesh;
        lodOneWaterObjectMeshCollider.sharedMesh = lodOneWaterObjectMeshFilter.mesh;
    }

    public void CreateMeshLodTwo()
    {
        //  -||-

        Mesh lodTwoMesh = new Mesh();
        lodTwoMesh.vertices = lodTwoVerticies.ToArray();

        lodTwoMesh.subMeshCount = 1;
        lodTwoMesh.SetTriangles(lodTwoTriangles.ToArray(), 0);

        lodTwoMesh.uv = lodTwoUvs.ToArray();

        lodTwoMesh.RecalculateNormals();

        lodTwoObjectMeshFilter.mesh = lodTwoMesh;
    }

    private void UpdateLodOneMeshData()
    {
        while (modifications.Count > 0)
        {
            VoxelMod vm = modifications.Dequeue();

            Vector3 pos = vm.position -= position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z] = vm.id;
        }

        for (int x = 0; x < VoxelData.chunkWidth; x++)
        {
            for (int z = 0; z < VoxelData.chunkWidth; z++)
            {
                tempTerrainHeight = 0;

                for (int y = 0; y < VoxelData.chunkHeight; y++)
                {
                    if (world.blocks[voxelMap[x, y, z]].drawBlock)
                    {
                        UpdateLodOneVoxelMeshData(new Vector3(x, y, z), tempTerrainHeight);
                    }
                }

                tempTerrainHeight = 0;
            }
        }
    }

    private void UpdateLodTwoMeshData()
    {
        //  Hardcoded square, with top vertices being terrai height at chunk local positon, using cached height value and bottom being 0.

        Block block = world.blocks[biome.surfaceBlockId];

        int halfChunkWidth = VoxelData.chunkWidth / 2;

        WorldTerrain.GetVoxelBlockType(world, new Vector3(position.x + halfChunkWidth, position.y, position.z + halfChunkWidth), false, 0);
        int highestY = tempTerrainHeight;

        if (highestY < 64)
        {
            highestY = 64;
            block = world.blocks[8];
        }

        int lowestY = 0;
        int chunkWidth = VoxelData.chunkWidth;

        #region top

        lodTwoVerticies.Add(new Vector3(0, highestY, 0)); //                   0
        lodTwoVerticies.Add(new Vector3(0, highestY, chunkWidth)); //          1
        lodTwoVerticies.Add(new Vector3(chunkWidth, highestY, 0)); //          2
        lodTwoVerticies.Add(new Vector3(chunkWidth, highestY, chunkWidth)); // 3

        AddTexture(block.GetTextureID(2), lodTwoUvs);

        lodTwoTriangles.Add(lodTwoVertexIndex + 0);
        lodTwoTriangles.Add(lodTwoVertexIndex + 1);
        lodTwoTriangles.Add(lodTwoVertexIndex + 2);
        lodTwoTriangles.Add(lodTwoVertexIndex + 2);
        lodTwoTriangles.Add(lodTwoVertexIndex + 1);
        lodTwoTriangles.Add(lodTwoVertexIndex + 3);

        lodTwoVertexIndex += 4;

        #endregion

        #region front

        lodTwoVerticies.Add(new Vector3(0, lowestY, 0)); //           0
        lodTwoVerticies.Add(new Vector3(chunkWidth, lowestY, 0)); //  1
        lodTwoVerticies.Add(new Vector3(0, highestY, 0)); //          2
        lodTwoVerticies.Add(new Vector3(chunkWidth, highestY, 0)); // 3

        AddTexture(block.GetTextureID(1), lodTwoUvs);

        lodTwoTriangles.Add(lodTwoVertexIndex + 2);
        lodTwoTriangles.Add(lodTwoVertexIndex + 1);
        lodTwoTriangles.Add(lodTwoVertexIndex + 0);
        lodTwoTriangles.Add(lodTwoVertexIndex + 3);
        lodTwoTriangles.Add(lodTwoVertexIndex + 1);
        lodTwoTriangles.Add(lodTwoVertexIndex + 2);

        lodTwoVertexIndex += 4;

        #endregion

        #region back

        lodTwoVerticies.Add(new Vector3(0, lowestY, chunkWidth)); //           0
        lodTwoVerticies.Add(new Vector3(chunkWidth, lowestY, chunkWidth)); //  1
        lodTwoVerticies.Add(new Vector3(0, highestY, chunkWidth)); //          2
        lodTwoVerticies.Add(new Vector3(chunkWidth, highestY, chunkWidth)); // 3

        AddTexture(block.GetTextureID(0), lodTwoUvs);

        lodTwoTriangles.Add(lodTwoVertexIndex + 0);
        lodTwoTriangles.Add(lodTwoVertexIndex + 1);
        lodTwoTriangles.Add(lodTwoVertexIndex + 2);
        lodTwoTriangles.Add(lodTwoVertexIndex + 2);
        lodTwoTriangles.Add(lodTwoVertexIndex + 1);
        lodTwoTriangles.Add(lodTwoVertexIndex + 3);

        lodTwoVertexIndex += 4;

        #endregion

        #region right

        lodTwoVerticies.Add(new Vector3(chunkWidth, lowestY, chunkWidth)); //  0
        lodTwoVerticies.Add(new Vector3(chunkWidth, lowestY, 0)); //           1
        lodTwoVerticies.Add(new Vector3(chunkWidth, highestY, chunkWidth)); // 2
        lodTwoVerticies.Add(new Vector3(chunkWidth, highestY, 0)); //          3

        AddTexture(block.GetTextureID(5), lodTwoUvs);

        lodTwoTriangles.Add(lodTwoVertexIndex + 0);
        lodTwoTriangles.Add(lodTwoVertexIndex + 1);
        lodTwoTriangles.Add(lodTwoVertexIndex + 2);
        lodTwoTriangles.Add(lodTwoVertexIndex + 2);
        lodTwoTriangles.Add(lodTwoVertexIndex + 1);
        lodTwoTriangles.Add(lodTwoVertexIndex + 3);

        lodTwoVertexIndex += 4;

        #endregion

        #region left

        lodTwoVerticies.Add(new Vector3(0, lowestY, 0)); //           0
        lodTwoVerticies.Add(new Vector3(0, lowestY, chunkWidth)); //  1
        lodTwoVerticies.Add(new Vector3(0, highestY, 0)); //          2
        lodTwoVerticies.Add(new Vector3(0, highestY, chunkWidth)); // 3

        AddTexture(block.GetTextureID(4), lodTwoUvs);

        lodTwoTriangles.Add(lodTwoVertexIndex + 0);
        lodTwoTriangles.Add(lodTwoVertexIndex + 1);
        lodTwoTriangles.Add(lodTwoVertexIndex + 2);
        lodTwoTriangles.Add(lodTwoVertexIndex + 2);
        lodTwoTriangles.Add(lodTwoVertexIndex + 1);
        lodTwoTriangles.Add(lodTwoVertexIndex + 3);

        lodTwoVertexIndex += 4;

        #endregion
    }

    private void UpdateLodOneVoxelMeshData(Vector3 pos, int _tempTerrainHeight)
    {
        //  Goes through each face of voxel
        //  Checking for parameters
        //  If not, create verticies, uvs and triangles for face

        byte blockID = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];

        bool isSolid = world.blocks[blockID].isSolid;
        bool isTransparent = world.blocks[blockID].isTransparent;
        bool isLiquid = world.blocks[blockID].isLiquid;

        for (int r = 0; r < 6; r++) // Each row (face)
        {
            bool skip = false;

            if (isLiquid) // If we are liquid, check if adjacent also is
            {
                if (CheckVoxelLiquid(pos + VoxelData.faceChecks[r], _tempTerrainHeight))
                {
                    skip = true;
                }
            }

            if (!skip && CheckVoxelTransparancy(pos + VoxelData.faceChecks[r], _tempTerrainHeight))   //  If we survived the parameters, draw face
            {
                //  Adding the uvs, the triangles and verticies, depending on mesh type, then incrementing the vertex index

                if (isLiquid)
                {
                    lodOneWaterVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 0]]);
                    lodOneWaterVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 1]]);
                    lodOneWaterVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 2]]);
                    lodOneWaterVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 3]]);

                    AddWaterTexture(pos, lodOneWaterUvs);

                    lodOneWaterTriangles.Add(lodOneWaterVertexIndex);
                    lodOneWaterTriangles.Add(lodOneWaterVertexIndex + 1);
                    lodOneWaterTriangles.Add(lodOneWaterVertexIndex + 2);
                    lodOneWaterTriangles.Add(lodOneWaterVertexIndex + 2);
                    lodOneWaterTriangles.Add(lodOneWaterVertexIndex + 1);
                    lodOneWaterTriangles.Add(lodOneWaterVertexIndex + 3);

                    lodOneWaterVertexIndex += 4;
                }

                else if (isTransparent)
                {
                    lodOneVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 0]]);
                    lodOneVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 1]]);
                    lodOneVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 2]]);
                    lodOneVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 3]]);

                    AddTexture(world.blocks[blockID].GetTextureID(r), lodOneUvs);

                    lodOneTransparentTriangles.Add(lodOneVertexIndex);
                    lodOneTransparentTriangles.Add(lodOneVertexIndex + 1);
                    lodOneTransparentTriangles.Add(lodOneVertexIndex + 2);
                    lodOneTransparentTriangles.Add(lodOneVertexIndex + 2);
                    lodOneTransparentTriangles.Add(lodOneVertexIndex + 1);
                    lodOneTransparentTriangles.Add(lodOneVertexIndex + 3);

                    lodOneVertexIndex += 4;
                }

                else if (isSolid)
                {
                    lodOneVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 0]]);
                    lodOneVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 1]]);
                    lodOneVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 2]]);
                    lodOneVerticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 3]]);

                    AddTexture(world.blocks[blockID].GetTextureID(r), lodOneUvs);

                    lodOneSolidTriangles.Add(lodOneVertexIndex);
                    lodOneSolidTriangles.Add(lodOneVertexIndex + 1);
                    lodOneSolidTriangles.Add(lodOneVertexIndex + 2);
                    lodOneSolidTriangles.Add(lodOneVertexIndex + 2);
                    lodOneSolidTriangles.Add(lodOneVertexIndex + 1);
                    lodOneSolidTriangles.Add(lodOneVertexIndex + 3);

                    lodOneVertexIndex += 4;
                }

                else
                {
                    Debug.Log("Error in Chunk/UpdateMeshData: Did not find mesh type");
                }
            }
        }
    }

    private void ClearMeshDataLodOne()
    {
        lodOneVertexIndex = 0;
        lodOneVerticies.Clear();

        lodOneSolidTriangles.Clear();
        lodOneTransparentTriangles.Clear();

        lodOneUvs.Clear();


        lodOneWaterVertexIndex = 0;
        lodOneWaterVerticies.Clear();

        lodOneWaterTriangles.Clear();

        lodOneWaterUvs.Clear();
    }

    private void ClearMeshDataLodTwo()
    {
        lodTwoVertexIndex = 0;

        lodTwoVerticies.Clear();

        lodTwoTriangles.Clear();

        lodTwoUvs.Clear();
    }

    private void AddTexture(int textureID, List<Vector2> uvs)
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

    private void AddWaterTexture(Vector3 pos, List<Vector2> uvs)
    {
        float step = 1f / (float)VoxelData.WorldSizeInVoxel;

        float x = ((VoxelData.chunkWidth * coord.x) + pos.x) * step;
        float z = ((VoxelData.chunkWidth * coord.z) + pos.z) * step;

        uvs.Add(new Vector2(x, z));
        uvs.Add(new Vector2(x, (z + step)));
        uvs.Add(new Vector2((x + step), z));
        uvs.Add(new Vector2((x + step), (z + step)));
    }

    #region util

    public byte GetVoxelFromGlobalVector3(Vector3 pos)
    {
        //  -||-

        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        if (xCheck < 0) xCheck = 0;
        if (zCheck < 0) zCheck = 0;

        return voxelMap[xCheck, yCheck, zCheck];
    }

    private bool CheckVoxelTransparancy(Vector3 pos, int _tempTerrainHeight)
    {
        //  Converts into ints
        //  Checks if voxel pos is inside of chunk
        //  Checks the position of given coordinates for voxel
        //  Returns true if voxel is there and the voxel is not transparent

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
        {
            return Voxel_Util.CheckIfVoxelTransparent(world, pos + position, _tempTerrainHeight);
        }

        return world.blocks[voxelMap[x, y, z]].isTransparent;
    }

    private bool CheckVoxelLiquid(Vector3 pos, int _tempTerrainHeight)
    {
        //  Converts into ints
        //  Checks if voxel pos is inside of chunk
        //  Checks the position of given coordinates for voxel
        //  Returns true if voxel is there and the voxel is not liquid

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
        {
            return Voxel_Util.CheckIfVoxelLiquid(world, pos + position, _tempTerrainHeight);
        }

        return world.blocks[voxelMap[x, y, z]].isLiquid;
    }

    private bool IsVoxelInChunk(int x, int y, int z)
    {
        //  If voxel is insite the perimiter of the chunk, return true

        if (x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1)
            return false;
        else
            return true;
    }

    #endregion
}