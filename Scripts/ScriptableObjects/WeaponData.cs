using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapon System/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;   
    public Sprite weaponIcon;      
    public int damage;             
    public float attackSpeed;     
    public string normalAttackTrigger; 
    public WeaponType weaponType;     
}

public enum WeaponType
{
    Sword,
    Bow,
    Hammer,
    Rifle,
    Pistol,
    Scythe
}
