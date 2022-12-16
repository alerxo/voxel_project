using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerConnection : NetworkBehaviour
{
    private int _gameMode;
    [SerializeField] private NetworkManager networkManager;
    

    private void Start()
    {
        _gameMode = PlayerPrefs.GetInt("GameMode");

        if (_gameMode == 0)
        {
            NetworkManager.Singleton.StartHost();
            StartGame();
        }
            
        else if (_gameMode == 1)
        {
            NetworkManager.Singleton.StartServer();
            StartGame();
        }
            
        else if (_gameMode == 2)
        {
            NetworkManager.Singleton.StartClient();
            StartGame();
        }
            

        else
            Debug.Log("Error with playerspawner");
    }

    private void StartGame()
    {
        
    }
}
