using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ChestController : NetworkBehaviour
{
    public Animator chestAnimator;
    public GameObject weaponUIPanel;
    public GameObject weaponCardPrefab; 
    public Transform weaponCardContainer; 
    public List<WeaponData> availableWeapons; 

    private bool isOpened = false;
    private bool isBeingDestroyed = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((collision.CompareTag("Weapon") || collision.CompareTag("Bullet") || collision.CompareTag("Player")) && !isOpened)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                chestAnimator.SetBool("IsOpened", true);
                ShowRandomWeapons();
                isOpened = true;
                return;
            }
            
            if (IsServer)
            {
                OpenChestClientRpc();
            }
            else if (IsClient)
            {
                RequestOpenChestServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOpenChestServerRpc()
    {
        if (!isOpened)
        {
            OpenChestClientRpc();
        }
    }

    [ClientRpc]
    private void OpenChestClientRpc()
    {
        if (isOpened) return;
        
        chestAnimator.SetBool("IsOpened", true);
        ShowRandomWeapons();
        isOpened = true;
    }

    private void ShowRandomWeapons()
    {
        if (weaponUIPanel == null || weaponCardPrefab == null || weaponCardContainer == null)
        {
            Debug.LogError("Referanslar eksik! Lütfen kontrol edin.");
            return;
        }

        weaponUIPanel.SetActive(true);

        foreach (Transform child in weaponCardContainer)
        {
            Destroy(child.gameObject);
        }

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

        if (!isBeingDestroyed && NetworkManager.Singleton.IsListening)
        {
            DestroyChestServerRpc();
            isBeingDestroyed = true;
        }
        else if (!NetworkManager.Singleton.IsListening)
        {
            Destroy(gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyChestServerRpc()
    {
        if (isBeingDestroyed) return;
        
        isBeingDestroyed = true;
        DestroyChestClientRpc();
        
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    [ClientRpc]
    private void DestroyChestClientRpc()
    {
        if (weaponUIPanel != null)
        {
            weaponUIPanel.SetActive(false);
        }
        
        isBeingDestroyed = true;
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
            Debug.LogWarning("weaponCardContainer referansı eksik!");
        }
    }
}
