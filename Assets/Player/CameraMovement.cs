using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;
    [SerializeField] private CinemachineFreeLook cinemachineFreeLook;
    private Transform player;

    public void AssingCamera(Transform target)
    {
        //  Waits for player to assign itself

        cinemachineFreeLook.Follow = target.Find("Camera Controls").Find("Camera Follow");
        cinemachineFreeLook.LookAt = target.Find("Camera Controls").Find("Camera Lookat");

        player = target;
    }

    public void AlignPlayerWithCamera()
    {
        player.transform.eulerAngles = new Vector3(player.eulerAngles.x, playerCamera.eulerAngles.y, player.eulerAngles.z);
    }
}
