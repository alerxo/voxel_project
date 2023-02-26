using UnityEngine;

[CreateAssetMenu(fileName = "Lode", menuName = "Modifications/Lode")]
public class Lode : ScriptableObject
{
    public string lodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
}
