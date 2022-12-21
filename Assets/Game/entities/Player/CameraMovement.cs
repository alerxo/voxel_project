using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CameraMovement : NetworkBehaviour
{
    [SerializeField] private Transform playerCamera;
    [SerializeField] private CinemachineFreeLook cinemachineFreeLook;
    [SerializeField] private Transform player;

    public void AlignPlayerWithCamera()
    {
        player.transform.eulerAngles = new Vector3(player.eulerAngles.x, playerCamera.eulerAngles.y, player.eulerAngles.z);
    }
}
