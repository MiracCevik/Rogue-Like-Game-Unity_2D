using System.Collections.Generic;

[System.Serializable]
public class CharacterStats
{
    public int currentHealth; 
    public int maxHealth;     
    public int attackDamage;  
    public int armor;         
    public int abilityPower;
    public string weapon = "Sword";
    public int gold;
    public List<string> unlockedSkills = new List<string>();

    public void InitializeFromDefault(DefaultCharacterStats defaultStats)
    {
        currentHealth = defaultStats.maxHealth;
        maxHealth = defaultStats.maxHealth;
        attackDamage = defaultStats.attackDamage;
        armor = defaultStats.armor;
        abilityPower=defaultStats.abilityPower;
        weapon=defaultStats.defaultWeapon;
        gold = defaultStats.gold;
        unlockedSkills.Clear();
    }

}
