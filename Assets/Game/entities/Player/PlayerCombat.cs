using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : NetworkBehaviour
{
    //  Input
    private ThirdPersonActionAsset thirdPersonActionAsset;

    public override void OnNetworkSpawn()
    {

        if (!IsOwner) return;

        thirdPersonActionAsset = new ThirdPersonActionAsset();

        thirdPersonActionAsset.Player.Attack.started += Attack_started;

        thirdPersonActionAsset.Player.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        thirdPersonActionAsset.Player.Attack.started -= Attack_started;

        thirdPersonActionAsset.Player.Disable();
    }

    private void Attack_started(InputAction.CallbackContext obj)
    {
        Debug.Log("Attacked (very scary)");
    }
}
