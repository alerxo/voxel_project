using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public GameObject chunkObject { get; private set; }
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private MeshCollider meshCollider;

    public ChunkCoordinate coord;

    private int vertexIndex = 0;
    private List<Vector3> verticies = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<int> transparentTriangles = new List<int>();
    private Material[] materials = new Material[2];
    private List<Vector2> uvs = new List<Vector2>();

    public Vector3 position;

    public byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    private World world;

    private bool _isActive;
    private bool isVoxelMapPopulated = false;

    // child objects

    public List<GameObject> childObjects = new List<GameObject>();

    public Chunk(ChunkCoordinate _coord, World _world)
    {
        coord = _coord;
        world = _world;

        IsActive = false;
    }

    public void Initialize()
    {
        //  Adds an inactove gameobject and sets it to ground layer
        chunkObject = new GameObject();
        IsActive = false;
        chunkObject.layer = 8;

        //  components

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshCollider = chunkObject.AddComponent<MeshCollider>();

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;

        meshRenderer.materials = materials;

        //  Parents to world obj and changes name

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.chunkWidth, 0f, coord.z * VoxelData.chunkWidth);

        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;

        position = chunkObject.transform.position;

        //  Tells world to populate this chunk, not doing it here cus we do it on another thread

        world.chunksToPopulate.Add(this);
    }

    public void PopulateVoxelMap()
    {
        //  Fills the chunk with voxel data
        //  Locks the chunksupdate list and adds itself, as to avoid confuction between threads

        for (int y = 0; y < VoxelData.chunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.chunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    voxelMap[x, y, z] = world.GetVoxelBlockType(new Vector3(x, y, z) + position);
                }
            }
        }

        isVoxelMapPopulated = true;

        lock (world.ThreadedUpdate_Lock)
        {
            world.chunksToUpdate.Add(this);
        }
    }

    public void UpdateChunk()
    {
        //  Adds all the modifications to the chunk, ie changing block id
        //  Adding voxel mesh data at each spot in the chunk if its a solid
        //  Adds itself to draw queue, threading -||-

        ClearMeshData();

        while(modifications.Count > 0)
        {
            VoxelMod vm = modifications.Dequeue();

            Vector3 pos = vm.position -= position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z] = vm.id;
        }

        for (int y = 0; y < VoxelData.chunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.chunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    if (world.blocks[voxelMap[x, y, z]].isSolid)
                        UpdateMeshData(new Vector3(x, y, z));
                }
            }
        }

        world.chunksToDraw.Enqueue(this);

    }

    private void ClearMeshData()
    {
        vertexIndex = 0;
        verticies.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
    }
    
    public bool IsActive
    {
        //  If changing the activate state, will update child objects, ie entities in chunk,
        //  related to render distance we want to render entities in chunk with chunk

        get { return _isActive; }

        set 
        { 
            _isActive = value;
            
            if(chunkObject != null)
            {
                chunkObject.SetActive(value);

                foreach(GameObject child in childObjects)
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

    private bool IsVoxelInChunk(int x, int y, int z)
    {
        //  If voxel is insite the perimiter of the chunk, return true

        if (x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1)
            return false;
        else 
            return true;
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
            world.chunksToUpdate.Insert(0, this);
            UpdateAdjacentVoxels(xCheck, yCheck, zCheck);
        }
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
                world.chunksToUpdate.Insert(0, world.GetChunkFromVector3(currentVoxel + position));

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

        if (!IsVoxelInChunk(x, y, z))
            return world.CheckIfVoxelTransparent(pos + position);

        return world.blocks[voxelMap[x, y, z]].isTransparent;
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos)
    {
        //  -||-

        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return voxelMap[xCheck, yCheck, zCheck];
    }

    private void UpdateMeshData(Vector3 pos)
    {
        //  Goes through each face of voxel
        //  Checking if there is something adjacent
        //  If not, create verticies, uvs and triangles for face

        byte blockID = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];
        bool isTransparent = world.blocks[blockID].isTransparent;

        for (int r = 0; r < 6; r++) // Each row (face)
        {
            if(CheckVoxel(pos + VoxelData.faceChecks[r])) 
            {
                //  Adding the verticies

                verticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 0]]);
                verticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 1]]);
                verticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 2]]);
                verticies.Add(pos + VoxelData.voxelVerticies[VoxelData.voxelTriangles[r, 3]]);

                //  Adding the uvs

                AddTexture(world.blocks[blockID].textureID);

                //  Adding the triangles, either to normal or transparent

                if (!isTransparent)
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }

                else
                {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                }


                //  Incrementing the vertex index

                vertexIndex += 4;
            }
        }
    }

    public void CreateMesh()
    {
        //  Adding the info to a mesh
        //  recalculating normals as to make sure all verticies face the right way

        Mesh mesh = new Mesh();
        mesh.vertices = verticies.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);

        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = meshFilter.mesh;
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
