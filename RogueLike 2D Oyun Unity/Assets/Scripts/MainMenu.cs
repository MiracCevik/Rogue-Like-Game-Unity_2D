using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private string gameSceneName = "Base";

    public void PlaySinglePlayer()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowMultiplayerOptions()
    {
        mainMenuPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        mainMenuPanel.SetActive(true);
        multiplayerPanel.SetActive(false);
    }

    public void HostGame()
    {
        NetworkManager.Singleton.StartHost();
        SceneManager.LoadScene("Base");
    }

    public void JoinGame()
    {
        NetworkManager.Singleton.StartClient();
    }
}

