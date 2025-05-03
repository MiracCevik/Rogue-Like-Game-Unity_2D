using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class OnlineGameManager : MonoBehaviour
{
    public static OnlineGameManager Instance { get; private set; }
    
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button serverButton;

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

    private void Start()
    {
        if (hostButton != null) hostButton.onClick.AddListener(() => StartHost());
        if (clientButton != null) clientButton.onClick.AddListener(() => StartClient());
        if (serverButton != null) serverButton.onClick.AddListener(() => StartServer());
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }
} 