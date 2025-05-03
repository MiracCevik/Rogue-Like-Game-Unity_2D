using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public DefaultCharacterStats defaultStats; 
    public CharacterStats characterStats;

    private void Start()
    {
    }

    public void ResetToDefaultStats()
    {
        characterStats.currentHealth = defaultStats.maxHealth;
        characterStats.maxHealth = defaultStats.maxHealth; 
        characterStats.attackDamage = defaultStats.attackDamage; 
        characterStats.armor = defaultStats.armor; 
        characterStats.abilityPower = defaultStats.abilityPower; 
        characterStats.weapon= defaultStats.defaultWeapon;
        characterStats.gold= defaultStats.gold;

        Debug.Log("Statlar varsayýlan deðerlere sýfýrlandý.");
    }


    public void TakeDamage(int damage)
    {
        characterStats.currentHealth -= damage;
        if (characterStats.currentHealth <= 0)
        {
            characterStats.currentHealth = 0;
            CharacterDeath(); 
        }
    }

    private void CharacterDeath()
    {
        Debug.Log("Karakter öldü!");
        ResetToDefaultStats(); 
        SaveManager.Instance.SaveGame();
        LoadMainMenu(); 
    }

    private void LoadMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
