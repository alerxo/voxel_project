using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ActivateCamera : MonoBehaviour
{
    [SerializeField] private GameObject cameraObject;

    private void Awake()
    {
        //  Assings itself to camera on spawn

        GameObject camera = Instantiate(cameraObject);

        camera.GetComponent<CameraMovement>().AssingCamera(transform);
    }
}
