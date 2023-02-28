using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class World_Generation
{
    public static void GenerateWorld(World world)
    {
        bool isTestScene = GameObject.FindGameObjectWithTag("TestSceneObject") != null;

        Debug.Log("Generating");

        ScrambleWorldSeed();
        CreateApplicationFilePathReference();
        CreateFog();

        Debug.Log("Prerequisites done");

        CreateBiomeMap(world);

        Debug.Log("Biome map done");

        CreateChunkObjects(world);

        Debug.Log("Chunk objects created");

        if (isTestScene) return;

        CreateTowns(world);
        ConnectTowns();

        Debug.Log("Towns created");

        CreateDungeons(world);

        Debug.Log("Dungeons created");

        Debug.Log("Done");
    }

    private static void ScrambleWorldSeed()
    {
        World_Settings.ScrambleSeed();
    }

    private static void CreateApplicationFilePathReference()
    {
        //  So that seperate threads can accses the path

        VoxelData.structureFilePath = Application.dataPath;
    }

    private static void CreateFog()
    {
        //  Sets the fog depending on the renderdistance

        RenderSettings.fogStartDistance = (World_Settings.RenderDistanceInChunks * VoxelData.chunkWidth) - VoxelData.chunkWidth * 6;
        RenderSettings.fogEndDistance = (World_Settings.RenderDistanceInChunks * VoxelData.chunkWidth) - VoxelData.chunkWidth * 2;
    }


    private static void CreateBiomeMap(World world)
    {
        world.subBiomeMap = BiomeMap.GetBiomeMap(world);
    }

    private static void CreateChunkObjects(World world)
    {
        //  starting from middle of world, ie half of the width in x and z
        //  Remove render distance value form starting loop and add it to ending loop,
        //  so we get square with width 2 renderdistances, ie renderdistance ín all directions

        int worldSize = World_Settings.worldSizeInChunks;

        for (int x = 0; x < worldSize; x++)
        {
            for (int z = 0; z < worldSize; z++)
            {
                world.InitializeChunk(x, z);
            }
        }
    }


    private static void CreateTowns(World world)
    {
        //  Loops through towns and creates them

        foreach(Town town in world.towns)
        {
            for(int i = 0; i < town.amountOfTowns; i++)
            {
                TownData townData = new TownData(world, town, GetRandomPosition());
                GenerateTown.GenerateTownData(townData);
            }
        }
    }

    private static void ConnectTowns()
    {

    }

    private static void CreateDungeons(World world)
    {
        //  Loops through dungeons and creates them

        foreach (Dungeon dungeon in world.dungeons)
        {
            for (int i = 0; i < dungeon.amountOfDungeons; i++)
            {
                Vector3 dungeonPosition = GetRandomPosition();
                Vector3 dungeonEntrancePosition = dungeonPosition;

                DungeonData dungeonData = new DungeonData(world, dungeon, dungeonPosition, dungeonEntrancePosition, 0);
                GenerateDungeon.GenerateDungeonData(dungeonData);
            }
        }
    }



    private static Vector3 GetRandomPosition()
    {
        Vector3 position = new Vector3();

        int edge = World_Settings.mapEdgeSizeInChunks * VoxelData.chunkWidth;
        int land = World_Settings.landSizeInChunks * VoxelData.chunkWidth;

        position.x = Random.Range(edge, edge + land);
        position.z = Random.Range(edge, edge + land);
        
        return position;
    }
}
