using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform player;

    public void AlignPlayerWithCamera()
    {
        player.transform.eulerAngles = new Vector3(player.eulerAngles.x, mainCamera.eulerAngles.y, player.eulerAngles.z);
    }
}
