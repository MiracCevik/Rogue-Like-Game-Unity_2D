using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        // Host butonu ayarları
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            UpdateUI("Host Başlatıldı!");
            DisableButtons();
        });

        // Client butonu ayarları
        clientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            UpdateUI("Sunucuya Bağlanılıyor...");
            DisableButtons();
        });

        // Network olaylarını dinle
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            UpdateUI($"Yeni Oyuncu Bağlandı! ID: {clientId}");
        }
        else
        {
            UpdateUI("Sunucuya Bağlanıldı!");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        UpdateUI("Bağlantı Kesildi!");
        EnableButtons();
    }

    private void DisableButtons()
    {
        hostButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
    }

    private void EnableButtons()
    {
        hostButton.gameObject.SetActive(true);
        clientButton.gameObject.SetActive(true);
    }

    private void UpdateUI(string message)
    {
        if (statusText) statusText.text = message;
    }
} 