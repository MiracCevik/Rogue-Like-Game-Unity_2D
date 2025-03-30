using UnityEngine;

public interface IEnemyAttack
{
    void ExecuteAttack(Animator animator, Transform player, EnemyStats stats, Transform enemyTransform, GameObject bulletPrefab);
}

public class MeleeAttack : IEnemyAttack
{
    KarakterHareket karakterHareket;

    public MeleeAttack(KarakterHareket karakterRef)
    {
        karakterHareket = karakterRef;
    }

    public void ExecuteAttack(Animator animator, Transform player, EnemyStats stats, Transform enemyTransform, GameObject bulletPrefab)
    {
        AttackRange attack = enemyTransform.GetComponent<AttackRange>();
        attack.MoveTowardsPlayer();
        animator.SetTrigger(stats.normalAttackTrigger);
    }
}

public class RangedAttack : IEnemyAttack
{
    KarakterHareket karakterHareket;
    public RangedAttack(KarakterHareket karakterRef)
    {
        karakterHareket = karakterRef;
    }

    public void ExecuteAttack(Animator animator, Transform player, EnemyStats stats, Transform enemyTransform, GameObject bulletPrefab)
    {
       
        animator.SetTrigger(stats.normalAttackTrigger);

        Vector2 direction = new Vector2(player.position.x - enemyTransform.position.x,0).normalized;
        GameObject bullet = GameObject.Instantiate(bulletPrefab, enemyTransform.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody2D>().velocity = direction * 10;
        
    }
}
