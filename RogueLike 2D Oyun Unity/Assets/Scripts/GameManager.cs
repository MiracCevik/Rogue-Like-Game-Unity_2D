using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    // IsOfflineMode olarak kullanılacak
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

    // Oyunu local host olarak başlatır (offline mod)
    public void StartLocalHostMode()
    {
        isLocalHostMode = true;
        
        // Eğer NetworkManager varsa, host olarak başlat
        if (NetworkManager.Singleton != null)
        {
            // Not connected ise host olarak başlat
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

    // Herhangi bir sunucuya bağlanmak için (gerçek online mod)
    public void StartClientMode()
    {
        isLocalHostMode = false;
        
        // Eğer NetworkManager varsa ve bağlı değilse client olarak başlat
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    // Oyunu kapatırken veya sahne değişirken NetworkManager'ı temizle
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