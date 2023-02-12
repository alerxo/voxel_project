using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveStructureData 
{
    public static void SaveData(Structure structure, VoxelModification[] voxelModifications)
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        string path = VoxelData.structureFilePath + "/Game/World/Structures/BinaryFiles/" + structure.structureName + ".bin";

        FileStream fileStream = new FileStream(path, FileMode.Create);

        StructureData structureData = new StructureData(voxelModifications);

        binaryFormatter.Serialize(fileStream, structureData);
        fileStream.Close();
    }

    public static StructureData LoadData(Structure structure)
    {
        string path = VoxelData.structureFilePath + "/Game/World/Structures/BinaryFiles/" + structure.structureName + ".bin";
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
            CreateDataFile(structure);
            return null;
        }
    }

    public static void CreateDataFile(Structure structure)
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        string path = VoxelData.structureFilePath + "/Game/World/Structures/BinaryFiles/" + structure.structureName + ".bin";

        FileStream fileStream = new FileStream(path, FileMode.Create);
        StructureData structureData = new StructureData();

        binaryFormatter.Serialize(fileStream, structureData);
        fileStream.Close();
    }
}
