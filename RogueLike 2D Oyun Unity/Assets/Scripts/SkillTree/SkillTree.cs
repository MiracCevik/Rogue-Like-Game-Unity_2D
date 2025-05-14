using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SkillTreeManager : MonoBehaviour
{
    [Header("Skill Tree Settings")]
    public GameObject skillTreePanel;
    public GameObject AeEPrefab;
    public TextMeshProUGUI goldText;
    public KarakterHareket karakter;
    public Transform character;
    private SkillData skillData;
   
    [Header("Skill List")]
    public List<SkillData> skills;
    public List<SkillIconController> skillIcons;
    private int playerGold;

    private bool isSkillTreeOpen = false;

    void Start()
    {
        playerGold = karakter.gold;

        for (int i = 0; i < skills.Count; i++)
        {
            if (i < skillIcons.Count)
            {
                skillIcons[i].Initialize(skills[i]);

            }
        }
    }

    void Update()
    {
        if (character != null)
        {
            transform.position = character.position ;
            transform.rotation = Quaternion.identity;
            Vector3 fixedScale = transform.localScale;
            fixedScale.x = Mathf.Abs(fixedScale.x);
            transform.localScale = fixedScale;
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (isSkillTreeOpen) CloseSkillTree();
            else OpenSkillTree();
        }
    }

    public void OpenSkillTree()
    {
        skillTreePanel.SetActive(true);
        Time.timeScale = 0f;
        isSkillTreeOpen = true;
    }

    public void CloseSkillTree()
    {
        skillTreePanel.SetActive(false);
        Time.timeScale = 1f;
        isSkillTreeOpen = false;
    }

    public void BuySkill(int skillIndex)
    {
        SkillData skill = skills[skillIndex];
        if (skill.isUnlocked)
        {
            return;
        }

        if (karakter.gold >= skill.cost)
        {
            karakter.gold -= skill.cost;
            skill.isUnlocked = true;
            skillIcons[skillIndex].ActivateSkillIcon();
            UpdateGoldUI(karakter.gold);
            SaveManager.Instance.SaveGame();
        }
       
    }

    public void UpdateGoldUI(int currentGold)
    {
        goldText.text = "Gold: " + currentGold.ToString();
    }

}
