using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public bool isLocalHostMode = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartLocalHostMode()
    {
        isLocalHostMode = true;
        
        if (NetworkManager.Singleton != null)
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                try {
                    NetworkManager.Singleton.StartHost();
                        Debug.Log("NetworkManager host olarak başlatıldı (Offline mod için)");
                }
                catch (System.Exception e) {
                    Debug.LogError($"Host başlatılırken hata: {e.Message}");
                }
            }
        }
        else
        {
            Debug.Log("NetworkManager bulunamadı, Offline modu NetworkManager olmadan devam ediyor");
        }
    }

    public void StartClientMode()
    {
        isLocalHostMode = false;
        
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    public void CleanupNetworkManager()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    private void OnDestroy()
    {
        CleanupNetworkManager();
    }
} 