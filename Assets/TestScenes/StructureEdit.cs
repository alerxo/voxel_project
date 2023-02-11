using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureEdit : MonoBehaviour
{
    private World world;

    private byte currentVoxelID;

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

        if(Input.GetKeyDown(KeyCode.E))
        {
            ChangeCurrentVoxelID();
        }
    }

    private void BuildVoxel()
    {
        RaycastHit raycastHit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out raycastHit, 10f))
        {
            Vector3 position = new Vector3(Mathf.FloorToInt(raycastHit.point.x), Mathf.FloorToInt(raycastHit.point.y), Mathf.FloorToInt(raycastHit.point.z));
            position = GetBuildVoxel(position, raycastHit.point);

            Chunk chunk = Voxel_Util.GetChunkFromVector3(world, position);
            chunk.EditVoxel(position, 3);

            Debug.Log("build: " + position);
        }
    }

    private Vector3 GetBuildVoxel(Vector3 position, Vector3 hit)
    {
        position += (hit - position).normalized;

        position = new Vector3(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));


        return position;
    }

    private void RemoveVoxel()
    {
        RaycastHit raycastHit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out raycastHit, 10f))
        {
            Vector3 position = new Vector3(Mathf.FloorToInt(raycastHit.point.x), Mathf.FloorToInt(raycastHit.point.y), Mathf.FloorToInt(raycastHit.point.z));
            position = GetRemoveVoxel(position, raycastHit.point);

            Chunk chunk = Voxel_Util.GetChunkFromVector3(world, position);
            chunk.EditVoxel(position, 0);

            Debug.Log("remove: " + position);
        }
    }

    private Vector3 GetRemoveVoxel(Vector3 position, Vector3 hit)
    {
        //position += (hit - position).normalized;

        position = new Vector3(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));


        return position;
    }

    private void ChangeCurrentVoxelID()
    {
        
    }
}
