using UnityEngine;
using UnityEngine.UI;

public class WeaponButton : MonoBehaviour
{
    public WeaponData weaponData; 
    public GameObject weapon;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        KarakterHareket karakter = FindObjectOfType<KarakterHareket>();
        if (karakter != null && weaponData != null)
        {
            SpriteRenderer weaponRenderer = weapon.GetComponent<SpriteRenderer>();
            if (weaponRenderer != null && weaponData.weaponIcon != null)
            {
                weaponRenderer.sprite = weaponData.weaponIcon;
            }
            karakter.EquipWeapon(weaponData);

        }
       
    }

}


