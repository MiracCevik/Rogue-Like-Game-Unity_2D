using UnityEngine;

public class StatManager : MonoBehaviour
{
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public int AttackDamage { get; private set; }
    public int Armor { get; private set; }
    public int AbilityPower { get; private set; }
    public CharacterStats characterStats = new CharacterStats();

    public void InitializeStats(DefaultCharacterStats defaultStats)
    {
        MaxHealth = defaultStats.maxHealth;
        CurrentHealth = MaxHealth;
        AttackDamage = defaultStats.attackDamage;
        Armor = defaultStats.armor;
        AbilityPower = defaultStats.abilityPower;
        characterStats.InitializeFromDefault(defaultStats);
    }
    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
    }

    public void UpdateStat(string statName, int value)
    {
        switch (statName)
        {
            case "AttackDamage":
                AttackDamage += value;
                break;
            case "Armor":
                Armor += value;
                break;
            case "AbilityPower":
                AbilityPower += value;
                break;
        }
    }
    public CharacterStats GetCharacterStats()
    {
        return characterStats;
    }

    public void LoadCharacterStats(CharacterStats stats)
    {
        characterStats = stats;
    }
}
