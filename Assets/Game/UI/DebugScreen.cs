using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    private RenderDistance RenderDistance;
    private TextMeshProUGUI text;

    private float frameRate;
    private float timer;

    private int halfWorldSizeInVoxel;
    private int halfWorldSizeInChunk;

    private void Start()
    {
        RenderDistance = GetComponent<RenderDistance>();
        text = GetComponent<TextMeshProUGUI >();

        halfWorldSizeInVoxel = VoxelData.WorldSizeInVoxel / 2;
        halfWorldSizeInChunk = VoxelData.WorldSizeInChunks / 2;
    }

    private void Update()
    {
        string debugtext = "voxel Project";
        debugtext += "\nversion: 0";
        debugtext += "\nfps: " + Fps();
        debugtext += "\ncoordinate: " + Coordinate();
        debugtext += "\nchunk: " + ChunkCoordinate();

        text.text = debugtext;

    }

    private string Fps()
    {
        if (timer > 1f)
        {
            //  Fps counter
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0f;
        }

        else
        {
            timer += Time.deltaTime;
        }

        return frameRate.ToString();
    }

    private string Coordinate()
    {
        //  World center isnt unity editor center, so we must take value minus half of world  

        return "xyz: " + (Mathf.FloorToInt(RenderDistance.transform.position.x) - halfWorldSizeInVoxel) + ", " + (Mathf.FloorToInt(RenderDistance.transform.position.y)) + ", " + (Mathf.FloorToInt(RenderDistance.transform.position.z) - halfWorldSizeInVoxel);
    }
    private string ChunkCoordinate()
    {
        //  -||-
        return "chunk: " + (RenderDistance.playerChunk.x - halfWorldSizeInChunk) + ", " + (RenderDistance.playerChunk.z - halfWorldSizeInChunk);
    }
}
