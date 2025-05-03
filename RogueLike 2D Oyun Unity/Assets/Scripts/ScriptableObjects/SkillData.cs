using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "SkillTree/Skills")]
public class SkillData : ScriptableObject
{
    public string skillName;    
    public int cost;            
    public bool isUnlocked = false;
    public float chargeTime ;
    public float damageMultiplier ;
    public float cooldown ;
    public SkillType skillType; 
    public Sprite icon;

    public enum SkillType
    {
        ChargeAttack,
        AoEAttack,
        Dash,
        Heal
    }
}
