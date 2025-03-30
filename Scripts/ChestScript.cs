using System.Collections.Generic;
using UnityEngine;

public class ChestController : MonoBehaviour
{
    public Animator chestAnimator;
    public GameObject weaponUIPanel;
    public GameObject weaponCardPrefab; 
    public Transform weaponCardContainer; 
    public List<WeaponData> availableWeapons; 

    private bool isOpened = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Weapon") || collision.CompareTag("Bullet"))
        {
            if (!isOpened)
            {
                chestAnimator.SetBool("IsOpened", true); 
                ShowRandomWeapons();
                isOpened = true; 
            }
        }
    }
    private void ShowRandomWeapons()
    {
        if (weaponUIPanel == null || weaponCardPrefab == null || weaponCardContainer == null)
        {
            Debug.LogError("Referanslar eksik! Lütfen kontrol edin.");
            return;
        }

        weaponUIPanel.SetActive(true); 

        List<WeaponData> selectedWeapons = new List<WeaponData>();

        while (selectedWeapons.Count < 3)
        {
            WeaponData randomWeapon = availableWeapons[Random.Range(0, availableWeapons.Count)];
            if (!selectedWeapons.Contains(randomWeapon))
            {
                selectedWeapons.Add(randomWeapon);
            }
        }

        foreach (WeaponData weapon in selectedWeapons)
        {
            GameObject weaponCard = Instantiate(weaponCardPrefab, weaponCardContainer);
            WeaponCard card = weaponCard.GetComponent<WeaponCard>();
            if (card != null)
            {
                card.SetupCard(weapon); 
            }
            else
            {
                Debug.LogError("WeaponCard script'i eksik!");
            }
        }
    }

    public void CloseChest()
    {
        if (weaponUIPanel != null)
        {
            weaponUIPanel.SetActive(false);
        }
        if (weaponCardContainer != null)
        {
            for (int i = weaponCardContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = weaponCardContainer.GetChild(i);
                Destroy(child.gameObject);
            }
        }
        else
        {
            Debug.LogWarning("weaponCardContainer referansý eksik!");
        }

        if (chestAnimator != null)
        {
            chestAnimator.SetBool("IsOpened", false);
        }

        isOpened = false;
    }
    public void HideAllCards()
    {
        if (weaponCardContainer != null)
        {
            foreach (Transform child in weaponCardContainer)
            {
                child.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("weaponCardContainer referansý eksik!");
        }
    }


}
