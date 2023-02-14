using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AtlasPacker : EditorWindow
{
    private int blockTextureSize = 16; //    Pixels
    private int atlasSizeInBlockTextures = 16;
    private int atlasSize;

    private Object[] rawTextures = new Object[256];
    private List<Texture2D> textureList = new List<Texture2D>();
    private Texture2D textureAtlas;

    [MenuItem("Voxel Project/Atlas Packer")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AtlasPacker));
    }

    private void OnGUI()
    {
        atlasSize = blockTextureSize * atlasSizeInBlockTextures;

        GUILayout.Label("Voxel Project Atlas Packer", EditorStyles.boldLabel);

        blockTextureSize = EditorGUILayout.IntField("Block Texture Size", blockTextureSize);
        atlasSizeInBlockTextures = EditorGUILayout.IntField("Atlas Size In Block Textures", atlasSizeInBlockTextures);

        GUILayout.Label(textureAtlas);

        if(GUILayout.Button("Load Textues"))
        {
            LoadTextures();
            PackAtlas();

            Debug.Log("Atlas Packer: Textures loaded.");
        }

        if(GUILayout.Button("Clear Textues"))
        {
            textureAtlas = new Texture2D(atlasSize, atlasSize);
            Debug.Log("Atlas Packer: Textures cleared.");
        }

        if(GUILayout.Button("Save Atlas"))
        {
            byte[] bytes = textureAtlas.EncodeToPNG();

            try
            {
                File.WriteAllBytes(Application.dataPath + "/Game/World/materials/Packed_Atlas.png", bytes);
                Debug.Log("Atlas Packer: successfully packed");
            }

            catch
            {
                Debug.Log("Atlas Packer: Couldn't save atlas to file.");
            }
        }
    }

    private void LoadTextures()
    {
        textureList.Clear();

        rawTextures = Resources.LoadAll("AtlasPacker", typeof(Texture2D));

        int index = 0;

        foreach (Object _texture in rawTextures)
        {
            Texture2D texture = (Texture2D)_texture;

            if (texture.width == blockTextureSize && texture.height == blockTextureSize)
            {
                textureList.Add(texture);
            }

            else
            {
                Debug.Log("Asset Packer: " + _texture.name + " incorrect size");
            }

            index++;
        }

        Debug.Log("Atlas Packer: " + textureList.Count + " successfully loaded");
    }

    private void PackAtlas()
    {
        textureAtlas = new Texture2D(atlasSize, atlasSize);
        Color[] pixels = new Color[atlasSize * atlasSize];

        for (int x = 0; x < atlasSize; x++)
        {
            for (int y = 0; y < atlasSize; y++)
            {
                int currentTextureX = x / blockTextureSize;
                int currentTextureY = y / blockTextureSize;

                int index = currentTextureY * atlasSizeInBlockTextures + currentTextureX;

                int currentPixelX = x - (currentTextureX * blockTextureSize);
                int currentPixelY = y - (currentTextureY * blockTextureSize);

                if(index < textureList.Count)
                {
                    pixels[(atlasSize - y - 1) * atlasSize + x] = textureList[index].GetPixel(x, blockTextureSize - y - 1);
                }

                else
                {
                    pixels[(atlasSize - y - 1) * atlasSize + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }

        textureAtlas.SetPixels(pixels);
        textureAtlas.Apply();
    }
}
