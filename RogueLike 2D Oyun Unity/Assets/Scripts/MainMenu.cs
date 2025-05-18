using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private string gameSceneName = "Base";
    [SerializeField] private string[] onlineScenes;

    public void PlaySinglePlayer()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CleanupNetworkManager();
            GameManager.Instance.StartLocalHostMode();
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void ShowMultiplayerOptions()
    {
        SelectRandomScene();
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartLocalHostMode();
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("GameManager bulunamadı!");
            NetworkManager.Singleton.StartHost();
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void JoinGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartClientMode();
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("GameManager bulunamadı!");
            NetworkManager.Singleton.StartClient();
        }
    }

    private void SelectRandomScene()
    {
      
        int randomElement = Random.Range(0, onlineScenes.Length);
        SceneManager.LoadScene(onlineScenes[randomElement]);
    }
}

