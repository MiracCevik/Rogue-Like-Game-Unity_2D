using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button offlineButton;
    [SerializeField] private Button onlineButton;
    [SerializeField] private GameObject modeIndicatorPanel;
    [SerializeField] private TextMeshProUGUI modeText;
    
    private void Start()
    {
        if (offlineButton != null)
        {
            offlineButton.onClick.AddListener(StartOfflineGame);
        }
        
        if (onlineButton != null)
        {
            onlineButton.onClick.AddListener(StartOnlineGame);
        }
        
        UpdateModeIndicator();
    }
    
    private void Update()
    {
        UpdateModeIndicator();
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMainMenu();
        }
    }
    
    private void StartOfflineGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartLocalHostMode();
            HideMainMenu();
            UpdateModeIndicator();
        }
    }
    
    private void StartOnlineGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartClientMode();
            HideMainMenu();
            UpdateModeIndicator();
        }
    }
    
    private void ToggleMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(!mainMenuPanel.activeSelf);
        }
    }
    
    private void HideMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }
    
    private void UpdateModeIndicator()
    {
        if (modeText != null && GameManager.Instance != null)
        {
            modeText.text = GameManager.Instance.isLocalHostMode ? 
                "Mod: Offline (Yerel Host)" : 
                "Mod: Online";
        }
    }
} 