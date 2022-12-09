using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;

    private void Start()
    {
        PlayerPrefs.SetInt("GameMode", 0);

        hostButton.onClick.AddListener(() =>
        {
            LoadGame(0);
        });

        serverButton.onClick.AddListener(() =>
        {
            LoadGame(1);
        });

        clientButton.onClick.AddListener(() =>
        {
            LoadGame(2);
        });
    }

    private void LoadGame(int _gameMode)
    {
        PlayerPrefs.SetInt("GameMode", _gameMode);

        SceneManager.LoadScene(1);
    }
}
