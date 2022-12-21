using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class PlayerMovement : NetworkBehaviour 
{
    private CharacterController characterController;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;

    private Vector3 velocity;

    [SerializeField] private GameObject playerCamera;
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private Transform movementDirection;

    private Vector3 movement;

    //  Input
    private ThirdPersonActionAsset thirdPersonActionAsset;

    //  Movement
    private float movementSpeed;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float crouchSpeed;

    [SerializeField] private float dashDuration;
    [SerializeField] private float dashSpeed;

    [SerializeField] private float jumpHeight;
    [SerializeField] private float gravitySpeed;

    public override void OnNetworkSpawn()
    {
        characterController = GetComponent<CharacterController>();

        if (!IsOwner) return;

        cameraMovement.gameObject.SetActive(true);
        thirdPersonActionAsset = new ThirdPersonActionAsset();

        thirdPersonActionAsset.Player.Jump.started += Jump_started;
        thirdPersonActionAsset.Player.Roll.started += Roll_started;

        thirdPersonActionAsset.Player.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        thirdPersonActionAsset.Player.Jump.started -= Jump_started;
        thirdPersonActionAsset.Player.Roll.started -= Roll_started;

        thirdPersonActionAsset.Player.Disable();
    }

    public void SetPlayerPositionAtSpawnPosition()
    {
        Vector3 spawnPosition = GameObject.FindGameObjectWithTag("SpawnObj").transform.position;
        transform.position = spawnPosition;
    }

    private void Update()
    {
        if (CanMove())
        {
            Movement();
            Gravity();
        }
    }

    private void Movement()
    {
        //  x,z movement
        //  We use camera y angle but player x angle so we store it in separate Transform (gameobject D:) 
        //  We multiply movementinput by this direction to get movement depending on camera rotation

        Vector2 inputDirection = thirdPersonActionAsset.Player.Movement.ReadValue<Vector2>();

        movementDirection.eulerAngles = new Vector3(transform.eulerAngles.x, playerCamera.transform.eulerAngles.y, transform.eulerAngles.z);

        movement = (movementDirection.transform.right * inputDirection.x + inputDirection.y * movementDirection.transform.forward).normalized;

        if (inputDirection.y > 0)
            cameraMovement.AlignPlayerWithCamera();

        if (thirdPersonActionAsset.Player.Run.IsPressed()) movementSpeed = runSpeed;
        else if (thirdPersonActionAsset.Player.Crouch.IsPressed()) movementSpeed = crouchSpeed;
        else movementSpeed = walkSpeed;

        characterController.Move(movementSpeed * Time.deltaTime * movement);
    }


    private void Roll_started(InputAction.CallbackContext obj)
    {
        if (CanMove() && IsGrounded())
        {
            StartCoroutine(_Dash());
        }
    }

    private IEnumerator _Dash()
    {
        //  If player is moving, dash in movement direction
        //  Else, dash where camera is looking
        //  Yields to rest of script

        float startTime = Time.time;


        while (Time.time < startTime + dashDuration)
        {
            if (movement != new Vector3(0, 0, 0))
                characterController.Move(movement * dashSpeed * Time.deltaTime);
            else
            {
                cameraMovement.AlignPlayerWithCamera();
                characterController.Move(transform.forward * dashSpeed * Time.deltaTime); 
            }
                

            yield return null;
        }

        velocity.x = 0;
    }

    private void Jump_started(InputAction.CallbackContext obj)
    {
        //  y movment

        if (IsGrounded())
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

        if(Physics.CheckBox(groundCheck.position, new Vector3(.5f, .1f, .5f), transform.rotation, groundMask))
            return true;
        else
            return false;
    }

    private bool CanMove()
    {
        //  If terrain exists under player, ie, chunk loaded

        if (Physics.Raycast(transform.position, -Vector3.up) && IsOwner)
            return true;

        else
            return false;
    }
}
