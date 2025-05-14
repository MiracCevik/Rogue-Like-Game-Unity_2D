using TMPro;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class WeaponCard : MonoBehaviour
{
    public Image weaponIcon;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI damageText; 
    public TextMeshProUGUI attackSpeedText; 
    private WeaponData weaponData;

    public void SetupCard(WeaponData data)
    {
        weaponData = data;

        if (weaponIcon != null)
            weaponIcon.sprite = weaponData.weaponIcon;

        if (weaponNameText != null)
            weaponNameText.text = weaponData.weaponName;

        if (damageText != null)
            damageText.text = $"Damage: {weaponData.damage}";

        if (attackSpeedText != null)
            attackSpeedText.text = $"Attack Speed: {weaponData.attackSpeed}";
    }

    public void OnSelectWeapon()
    {
        WeaponCard[] allCards = FindObjectsOfType<WeaponCard>();
        foreach (WeaponCard card in allCards)
        {
            card.gameObject.SetActive(false);
        }

        KarakterHareket[] allPlayers = FindObjectsOfType<KarakterHareket>();
        
        KarakterHareket localPlayer = null;
        foreach (KarakterHareket player in allPlayers)
        {
            if (player.IsOwner)
            {
                localPlayer = player;
                break;
            }
        }

        if (localPlayer != null)
        {
            
            if (NetworkManager.Singleton.IsListening)
            {
                string weaponName = weaponData.weaponName;
                int damage = weaponData.damage;
                float attackSpeed = weaponData.attackSpeed;
                int weaponTypeInt = (int)weaponData.weaponType;
                
                localPlayer.EquipWeaponServerRpc(weaponName, damage, attackSpeed, weaponTypeInt);
            }
            else
            {
                localPlayer.EquipWeapon(weaponData);
                SaveManager saveManager = FindObjectOfType<SaveManager>();
                if (saveManager != null)
                {
                    saveManager.SaveGame();
                }
            }
        }

        CloseChest();
    }

    private void CloseChest()
    {
        ChestController chestController = FindObjectOfType<ChestController>();
        if (chestController != null)
        {
            chestController.CloseChest();
        }
    }
}
