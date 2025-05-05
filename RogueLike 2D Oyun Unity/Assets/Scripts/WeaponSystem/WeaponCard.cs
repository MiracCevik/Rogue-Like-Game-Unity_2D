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
            Debug.Log($"{weaponData.weaponName} se�ildi ve karaktere donat�ld�!");

            SaveManager saveManager = FindObjectOfType<SaveManager>();
            if (saveManager != null)
            {
                saveManager.SaveGame();
                Debug.Log("Oyun kaydedildi.");
            }
            else
            {
                Debug.LogWarning("SaveManager bulunamad�!");
            }
        }
        else
        {
            Debug.LogWarning("Karakter bulunamad�!");
        }

        CloseChest();
    }

    private void CloseChest()
    {
        ChestController chestController = FindObjectOfType<ChestController>();
        if (chestController != null)
        {
            Debug.Log("weaponcard i�indeki close chest");
            chestController.CloseChest();
        }
        else
        {
            Debug.LogWarning("ChestController bulunamad�!");
        }
    }
}
