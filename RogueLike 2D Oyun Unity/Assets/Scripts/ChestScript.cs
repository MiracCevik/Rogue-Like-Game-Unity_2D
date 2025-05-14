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
    // Add a flag to track despawn status to prevent multiple attempts
    private bool isBeingDestroyed = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((collision.CompareTag("Weapon") || collision.CompareTag("Bullet") || collision.CompareTag("Player")) && !isOpened)
        {
            // Only server can initiate the chest opening sequence
            if (IsServer)
            {
                OpenChestClientRpc();
            }
            // When a client hits the chest, request the server to open it
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
        // Make sure we don't try to open the chest multiple times
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

        // Mevcut kartları temizle
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
        // Always hide UI panel regardless of network mode
        if (weaponUIPanel != null)
        {
            weaponUIPanel.SetActive(false);
        }

        // Always call the ServerRpc regardless of whether this is the server or not
        if (!isBeingDestroyed && NetworkManager.Singleton.IsListening)
        {
            DestroyChestServerRpc();
            isBeingDestroyed = true;
        }
        // For standalone mode or if network is not active
        else if (!NetworkManager.Singleton.IsListening)
        {
            Destroy(gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyChestServerRpc()
    {
        // This will only execute on the server
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
        // Ensure UI is closed on all clients
        if (weaponUIPanel != null)
        {
            weaponUIPanel.SetActive(false);
        }
        
        // Set flag so we don't try to destroy it again
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
