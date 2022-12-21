using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimations : NetworkBehaviour
{
    [SerializeField] private Animator playerAnimator;

    //  Input
    private ThirdPersonActionAsset thirdPersonActionAsset;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        thirdPersonActionAsset = new ThirdPersonActionAsset();

        thirdPersonActionAsset.Player.Jump.started += Jump_started;
        thirdPersonActionAsset.Player.Roll.started += Roll_started;
        thirdPersonActionAsset.Player.Attack.started += Attack_started;

        thirdPersonActionAsset.Player.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        thirdPersonActionAsset.Player.Jump.started -= Jump_started;
        thirdPersonActionAsset.Player.Roll.started -= Roll_started;
        thirdPersonActionAsset.Player.Attack.started -= Attack_started;

        thirdPersonActionAsset.Player.Disable();
    }

    private void Update()
    {
        if(!IsOwner) return;

        if (thirdPersonActionAsset.Player.Run.IsPressed()) playerAnimator.SetFloat("Movement", 0.5f);
        else if (thirdPersonActionAsset.Player.Crouch.IsPressed() && thirdPersonActionAsset.Player.Movement.ReadValue<Vector2>() == Vector2.zero) playerAnimator.SetFloat("Movement", 0.75f);
        else if (thirdPersonActionAsset.Player.Crouch.IsPressed()) playerAnimator.SetFloat("Movement", 1f);
        else if (thirdPersonActionAsset.Player.Movement.ReadValue<Vector2>() != Vector2.zero) playerAnimator.SetFloat("Movement", 0.25f);
        else playerAnimator.SetFloat("Movement", 0f);
    }

    private void Attack_started(InputAction.CallbackContext obj)
    {
        playerAnimator.SetTrigger("Attack");
    }

    private void Roll_started(InputAction.CallbackContext obj)
    {
        playerAnimator.SetTrigger("Roll");
    }

    private void Jump_started(InputAction.CallbackContext obj)
    {
        playerAnimator.SetTrigger("Jump");
    }
}
