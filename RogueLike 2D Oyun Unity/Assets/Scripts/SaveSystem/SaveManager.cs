using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerSaveData
{
    public int CharHealth;
    public int AttackDamage;
    public int AP;
    public int Armor;
    public float atackSpeed;
    public string SelectedWeapon;
    public int Gold;
    public List<string> UnlockedSkills = new List<string>();
}

public class SaveManager : MonoBehaviour
{
    public KarakterHareket karakterHareket;
    public WeaponData[] allWeapons;
    public static SaveManager Instance { get; private set; }

    private string saveFilePath;

    void Start()
    {   
        karakterHareket =   FindAnyObjectByType<KarakterHareket>();
        saveFilePath = Application.persistentDataPath + "/playerdata.json";
        LoadGame();
    }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SaveGame()
    {
        PlayerSaveData data = new PlayerSaveData
        {
            CharHealth = karakterHareket.CharHealth,
            AttackDamage = karakterHareket.attackDamage,
            AP = karakterHareket.abilityPower,
            Armor = karakterHareket.armor,
            atackSpeed = karakterHareket.attackSpeed,
            Gold = karakterHareket.gold,
            SelectedWeapon = karakterHareket.weaponsScript.weaponData != null
                ? karakterHareket.weaponsScript.weaponData.weaponName
                : "NoWeapon"
        };

        SkillTreeManager skillTreeManager = FindObjectOfType<SkillTreeManager>();
        foreach (SkillData skill in skillTreeManager.skills)
        {
            if (skill.isUnlocked)
            {
                data.UnlockedSkills.Add(skill.skillName);
            }
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);

        Debug.Log("Oyun ba�ar�yla kaydedildi: " + saveFilePath);
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("Kaydedilmi� dosya bulunamad�, varsay�lan de�erler kullan�lacak.");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);

        karakterHareket.CharHealth = data.CharHealth;
        karakterHareket.currentHealth = data.CharHealth;
        karakterHareket.attackDamage = data.AttackDamage;
        karakterHareket.abilityPower = data.AP;
        karakterHareket.armor = data.Armor;
        karakterHareket.gold = data.Gold;

        SkillTreeManager skillTreeManager = FindObjectOfType<SkillTreeManager>();
        if (skillTreeManager != null)
        {
            skillTreeManager.UpdateGoldUI(karakterHareket.gold);

            foreach (SkillData skill in skillTreeManager.skills)
            {
                skill.isUnlocked = data.UnlockedSkills.Contains(skill.skillName);
            }
        }

    }


}
