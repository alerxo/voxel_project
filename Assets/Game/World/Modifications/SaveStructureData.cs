using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveStructureData 
{
    public static void SaveData(Structure structure, VoxelModification[] voxelModifications)
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/StructureData.fun";

        FileStream fileStream = new FileStream(path, FileMode.Create);

        StructureData structureData = new StructureData(voxelModifications);

        binaryFormatter.Serialize(fileStream, structureData);
        fileStream.Close();
    }

    public static StructureData LoadData(Structure structure)
    {
        string path = Application.persistentDataPath + "/StructureData.fun";
        if (File.Exists(path))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream fileStream = new FileStream(path, FileMode.Open);

            StructureData structureData = binaryFormatter.Deserialize(fileStream) as StructureData;
            fileStream.Close();

            return structureData;
        }

        else
        {
            Debug.LogError("Save file not found for " + structure.name + " in" + path);
            CreateDataFile();
            return null;
        }
    }

    public static void CreateDataFile()
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/StructureData.fun";

        FileStream fileStream = new FileStream(path, FileMode.Create);
        StructureData structureData = new StructureData();

        binaryFormatter.Serialize(fileStream, structureData);
        fileStream.Close();
    }
}
