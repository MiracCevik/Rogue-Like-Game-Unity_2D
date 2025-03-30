using UnityEngine;

public enum AttackType
{
    Melee,
    Ranged,
    Boss
}

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Weapon System/Enemy")]
public class EnemyStats : ScriptableObject
{
    public string enemyName;
    public Sprite enemyIcon;
    public int enemyDamage;
    public int enemyHealth;
    public float attackSpeed;
    public float attackRange;
    public string normalAttackTrigger;
    public AttackType attackType;
    public float moveSpeed;
    public int rewards;
}
