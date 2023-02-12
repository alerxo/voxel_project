using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureEdit : MonoBehaviour
{
    private World world;

    [SerializeField] private byte currentVoxelID;

    [SerializeField] private Transform player;

    private void Awake()
    {
        world = GameObject.FindGameObjectWithTag("WorldObj").GetComponent<World>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            RemoveVoxel();
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            BuildVoxel();
        }
    }

    private void BuildVoxel()
    {
        RaycastHit raycastHit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out raycastHit, 10f))
        {
            if(currentVoxelID >= world.blocks.Length)
            {
                Debug.LogError("CurrentVoxelID out of bounds");
                return;
            }

            Vector3 position = new Vector3(Mathf.FloorToInt(raycastHit.point.x + raycastHit.normal.x / 2), Mathf.FloorToInt(raycastHit.point.y + raycastHit.normal.y / 2), Mathf.FloorToInt(raycastHit.point.z + raycastHit.normal.z / 2));

            Chunk chunk = Voxel_Util.GetChunkFromVector3(world, position);
            chunk.EditVoxel(position, currentVoxelID);
        }
    }

    private void RemoveVoxel()
    {
        RaycastHit raycastHit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out raycastHit, 10f))
        {
            Vector3 position = new Vector3(Mathf.FloorToInt(raycastHit.point.x - raycastHit.normal.x / 2), Mathf.FloorToInt(raycastHit.point.y - raycastHit.normal.y / 2), Mathf.FloorToInt(raycastHit.point.z - raycastHit.normal.z / 2)); ;
            
            Chunk chunk = Voxel_Util.GetChunkFromVector3(world, position);
            chunk.EditVoxel(position, 0);
        }
    }
}
