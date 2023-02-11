using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class StructureEditMovement : MonoBehaviour
{
    private Transform playerCamera;

    private CharacterController characterController;
    [SerializeField] private Transform movementDirection;

    private Vector3 movement;
    private Vector3 yVelocity;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = Camera.main.transform;
    }

    private void Update()
    {
        Movement();
        YMovement();

    }

    private void Movement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        movementDirection.eulerAngles = new Vector3(transform.eulerAngles.x, playerCamera.transform.eulerAngles.y, transform.eulerAngles.z);

        movement = (movementDirection.transform.right * horizontal + vertical * movementDirection.transform.forward).normalized;

        characterController.Move(movement * 10f * Time.deltaTime);
    }
    private void YMovement()
    {
        if(Input.GetKey(KeyCode.Space))
        {
            yVelocity.y = 10;
        }

        else if (Input.GetKey(KeyCode.LeftControl))
        {
            yVelocity.y = -10;
        }

        else
        {
            yVelocity.y = 0;
        }

        characterController.Move(yVelocity * Time.deltaTime);
    }
}
