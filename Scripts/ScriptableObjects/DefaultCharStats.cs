using UnityEngine;

[CreateAssetMenu(fileName = "DefaultCharacterStats", menuName = "Character/DefaultStats")]
public class DefaultCharacterStats : ScriptableObject
{
    public int maxHealth = 100;
    public int attackDamage = 0;
    public int armor = 0;
    public int abilityPower = 0;
    public string defaultWeapon = "Sword";
    public int gold = 0;

    
}


