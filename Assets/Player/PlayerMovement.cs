using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour 
{
    private float movementSpeed;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float crouchSpeed;

    [SerializeField] private float dashDuration;
    [SerializeField] private float dashSpeed;

    [SerializeField] private float jumpHeight;
    [SerializeField] private float gravitySpeed;

    private CharacterController characterController;
    [SerializeField] private Transform groundCheck;

    [SerializeField] private LayerMask groundMask;

    private Vector3 velocity;

    [SerializeField] private GameObject playerCamera;
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private Transform movementDirection;

    private Vector3 movement;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        SetPlayerPositionAtSpawnPosition();

        cameraMovement.gameObject.SetActive(true);
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
            Dash();
            Jump();
            Gravity();

            characterController.Move(velocity * Time.deltaTime);
        }
    }

    private void Movement()
    {
        //  x,z movement
        //  We use camera y angle but player x angle so we store it in separate Transform (gameobject D:) 
        //  We multiply movementinput by this direction to get movement depending on camera rotation

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        movementDirection.eulerAngles = new Vector3(transform.eulerAngles.x, playerCamera.transform.eulerAngles.y, transform.eulerAngles.z);

        movement = (movementDirection.transform.right * horizontal + vertical * movementDirection.transform.forward).normalized;

        if (vertical > 0)
            cameraMovement.AlignPlayerWithCamera();

        if (Input.GetKey(KeyCode.LeftShift)) movementSpeed = runSpeed;
        else if (Input.GetKey(KeyCode.LeftControl)) movementSpeed = crouchSpeed;
        else movementSpeed = walkSpeed;

        characterController.Move(movement * movementSpeed * Time.deltaTime);
    }

    private void Dash()
    {
        if (Input.GetKey(KeyCode.Q) && CanMove())
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
