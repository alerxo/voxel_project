using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Structure", menuName = "Modifications/Town")]
public class Town : ScriptableObject
{
    public string townName;

    public Structure[] townCenter;
    public Structure[] roads;
    public Structure[] houses;
}
