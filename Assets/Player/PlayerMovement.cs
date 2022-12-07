using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float gravitySpeed;

    private CharacterController characterController;
    [SerializeField] private Transform groundCheck;

    [SerializeField] private LayerMask groundMask;

    private Vector3 velocity;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (CanMove())
        {
            Movement();
            Jump();
            Gravity();
        }
    }

    private void Movement()
    {
        //  x,z movement

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(horizontal, 0, vertical);

        characterController.Move(movement * movementSpeed * Time.deltaTime);
    }

    private void Jump()
    {
        //  y movment

        if (Input.GetButtonDown("Jump") && CanJump())
            velocity.y = jumpHeight;
    }

    private void Gravity()
    {
        //  y movment

        if (IsGrounded() && velocity.y < 0)
        {
            velocity.y = -0.1f;
        }

        else if (velocity.y > -9.81f * 5)
        {
            velocity.y += -9.81f * gravitySpeed * Time.deltaTime;
        }

        characterController.Move(velocity * Time.deltaTime);
    }

    private bool IsGrounded()
    {
        //  Ground check obj

        if(Physics.CheckSphere(groundCheck.position, 0.1f, groundMask))
            return true;
        else
            return false;
    }

    private bool CanMove()
    {
        //  If terrain exists under player, ie, chunk loaded

        if (Physics.Raycast(transform.position, -Vector3.up))
            return true;

        else
            return false;
    }

    private bool CanJump()
    {
        if (IsGrounded())
            return true;
        else
            return false;
    }
}
