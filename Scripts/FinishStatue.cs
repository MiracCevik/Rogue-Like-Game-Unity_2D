using UnityEngine;
using TMPro;

public class FinishStatue : MonoBehaviour
{
    public GameObject winText; 
    public SaveManager saveManager; 

    private void Start()
    {
        if (winText != null)
        {
            winText.gameObject.SetActive(false);

            Debug.Log("Win text false hale getirildi!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {   

        if (collision.CompareTag("Player"))
        {
            if (winText != null)
            {
                winText.gameObject.SetActive(true);

                Debug.Log("Win text aktif hale getirildi!");
            }

            if (saveManager != null)
            {
                saveManager.SaveGame();
                Debug.Log("Oyun kaydedildi!");
            }
            else
            {
                Debug.LogError("SaveManager atanmamýþ!");
            }
        }
    }
}
