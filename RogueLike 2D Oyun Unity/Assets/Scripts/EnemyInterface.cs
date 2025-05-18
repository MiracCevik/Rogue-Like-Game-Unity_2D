using Unity.Netcode;
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
        if (player == null) return;
        if (animator != null && !string.IsNullOrEmpty(stats.normalAttackTrigger))
        {
            animator.SetTrigger(stats.normalAttackTrigger);
        }
        else if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        AttackRange attack = enemyTransform.GetComponent<AttackRange>();
        if (attack != null) 
        {
            attack.MoveTowardsPlayer(player);
        }
        else
        {
            float distance = Vector2.Distance(enemyTransform.position, player.position);
            
            if (distance > 1.0f)
            {
                Vector2 direction = (player.position - enemyTransform.position).normalized;
                enemyTransform.Translate(direction * stats.moveSpeed * Time.deltaTime);
            }
            else if (distance < 0.8f)
            {
                Vector2 direction = (enemyTransform.position - player.position).normalized;
                enemyTransform.Translate(direction * stats.moveSpeed * 0.5f * Time.deltaTime);
            }
        }
        
        float attackRadius = 1.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(enemyTransform.position, attackRadius);
        
        bool hasHitPlayer = false;
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                KarakterHareket playerTarget = hit.GetComponent<KarakterHareket>();
                if (playerTarget != null)
                {
                    bool isOfflineMode = GameManager.Instance != null && GameManager.Instance.isLocalHostMode;
                    int damage = stats.enemyDamage;
                    
                    if (isOfflineMode || !NetworkManager.Singleton.IsListening)
                    {
                        playerTarget.TakeDamage(damage);
                    }
                    hasHitPlayer = true;
                    break;
                }
            }
        }
        if (!hasHitPlayer && attack == null)
        {
            bool shouldFaceRight = player.position.x > enemyTransform.position.x;
            bool isCurrentlyFacingRight = enemyTransform.localScale.x > 0;
            
            if (shouldFaceRight != isCurrentlyFacingRight)
            {
                Vector3 scale = enemyTransform.localScale;
                scale.x *= -1;
                enemyTransform.localScale = scale;
            }
        }
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
        if (player == null) return;
        
        if (animator != null && !string.IsNullOrEmpty(stats.normalAttackTrigger))
        {
            animator.SetTrigger(stats.normalAttackTrigger);
        }
        else if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (bulletPrefab == null) return;
        bool shouldFaceRight = player.position.x > enemyTransform.position.x;
        bool isCurrentlyFacingRight = enemyTransform.localScale.x > 0;
        
        if (shouldFaceRight != isCurrentlyFacingRight)
        {
            Vector3 scale = enemyTransform.localScale;
            scale.x *= -1;
            enemyTransform.localScale = scale;
        }

        Vector2 direction = new Vector2(shouldFaceRight ? 1 : -1, 0);
        Vector3 spawnPosition = enemyTransform.position + new Vector3(direction.x * 0.5f, 0, 0);
        bool isOfflineMode = GameManager.Instance != null && GameManager.Instance.isLocalHostMode;
        
        if (isOfflineMode || !NetworkManager.Singleton.IsListening)
        {
            GameObject bullet = GameObject.Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            
            Vector3 bulletScale = bullet.transform.localScale;
            bulletScale.x = direction.x >= 0 ? Mathf.Abs(bulletScale.x) : -Mathf.Abs(bulletScale.x);
            bullet.transform.localScale = bulletScale;
            
            EnemyBullets bulletScript = bullet.GetComponent<EnemyBullets>();
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * 10;
            }
            else if (bulletScript != null)
            {
                bulletScript.moveDirection = direction;
            }
            
            if (bulletScript != null)
            {
                bulletScript.stats = stats;
                bulletScript.bulletDamage = stats.enemyDamage;
            }
            
            GameObject.Destroy(bullet, 4f);
        }
        else
        {
            bool isFlipped = direction.x < 0;
            GameObject bullet = GameObject.Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            EnemyBullets bulletScript = bullet.GetComponent<EnemyBullets>();
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * 10;
            }
            
            if (bulletScript != null)
            {
                bulletScript.stats = stats;
                bulletScript.bulletDamage = stats.enemyDamage;
            }
            
            if (isFlipped)
            {
                Vector3 bulletScale = bullet.transform.localScale;
                bulletScale.x = -Mathf.Abs(bulletScale.x);
                bullet.transform.localScale = bulletScale;
            }
            else
            {
                Vector3 bulletScale = bullet.transform.localScale;
                bulletScale.x = Mathf.Abs(bulletScale.x);
                bullet.transform.localScale = bulletScale;
            }
            
            if (bullet.GetComponent<NetworkObject>() == null)
            {
                GameObject.Destroy(bullet, 4f);
            }
        }
    }
}
