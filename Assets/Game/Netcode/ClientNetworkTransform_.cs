using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientNetworkTransform_ : NetworkTransform
{
    //  Gives client control of transform

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
