using TMPro;
using UnityEngine;
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
        KarakterHareket player = FindObjectOfType<KarakterHareket>();
        if (player != null)
        {
            player.EquipWeapon(weaponData);
            Debug.Log($"{weaponData.weaponName} seçildi ve karaktere donatýldý!");

            SaveManager saveManager = FindObjectOfType<SaveManager>();
            if (saveManager != null)
            {
                saveManager.SaveGame();
                Debug.Log("Oyun kaydedildi.");
            }
            else
            {
                Debug.LogWarning("SaveManager bulunamadý!");
            }
        }
        else
        {
            Debug.LogWarning("Karakter bulunamadý!");
        }

        CloseChest();
    }

    private void CloseChest()
    {
        ChestController chestController = FindObjectOfType<ChestController>();
        if (chestController != null)
        {
            Debug.Log("weaponcard içindeki close chest");
            chestController.CloseChest();
        }
        else
        {
            Debug.LogWarning("ChestController bulunamadý!");
        }
    }
}
