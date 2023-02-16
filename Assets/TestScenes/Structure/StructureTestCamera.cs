using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureTestCamera : MonoBehaviour
{
    private float xRotation = 0f;

    [SerializeField] private Transform player;

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * 100f * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        player.Rotate(Vector3.up * mouseX);
    }
}
