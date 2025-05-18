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
        
        // AnimatorController'ı kontrol et
        if (animator != null && !string.IsNullOrEmpty(stats.normalAttackTrigger))
        {
            animator.SetTrigger(stats.normalAttackTrigger);
        }
        else if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // AttackRange bileşeni ile oyuncuya hareket et
        AttackRange attack = enemyTransform.GetComponent<AttackRange>();
        if (attack != null) 
        {
            attack.MoveTowardsPlayer(player);
        }
        else
        {
            // AttackRange yoksa, düşmanı doğrudan oyuncuya doğru hareket ettir
            float distance = Vector2.Distance(enemyTransform.position, player.position);
            
            if (distance > 1.0f)
            {
                Vector2 direction = (player.position - enemyTransform.position).normalized;
                enemyTransform.Translate(direction * stats.moveSpeed * Time.deltaTime);
            }
            else if (distance < 0.8f) // Çok yakınsa biraz uzaklaş
            {
                Vector2 direction = (enemyTransform.position - player.position).normalized;
                enemyTransform.Translate(direction * stats.moveSpeed * 0.5f * Time.deltaTime);
            }
        }
        
        // Oyuncu yakınında mı kontrol et ve hasarı uygula
        float attackRadius = 1.5f; // Saldırı menzilini genişlet
        Collider2D[] hits = Physics2D.OverlapCircleAll(enemyTransform.position, attackRadius);
        
        bool hasHitPlayer = false;
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                KarakterHareket playerTarget = hit.GetComponent<KarakterHareket>();
                if (playerTarget != null)
                {
                    // Offline modda direkt hasar ver, online modda RPC kullan
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
        
        // Eğer oyuncuya vuramadıysak
        if (!hasHitPlayer && attack == null)
        {
            // Düşman yönünü her zaman oyuncuya doğru tutsun
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
        
        // AnimatorController'ı kontrol et
        if (animator != null && !string.IsNullOrEmpty(stats.normalAttackTrigger))
        {
            animator.SetTrigger(stats.normalAttackTrigger);
        }
        else if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (bulletPrefab == null) return;

        // Düşman yönünü oyuncuya doğru çevir
        bool shouldFaceRight = player.position.x > enemyTransform.position.x;
        bool isCurrentlyFacingRight = enemyTransform.localScale.x > 0;
        
        if (shouldFaceRight != isCurrentlyFacingRight)
        {
            Vector3 scale = enemyTransform.localScale;
            scale.x *= -1;
            enemyTransform.localScale = scale;
        }

        // Oyuncuya doğru yön hesapla
        Vector2 direction = (player.position - enemyTransform.position).normalized;
        
        // Merminin konumunu ayarla (düşmanın önünde)
        Vector3 spawnPosition = enemyTransform.position + new Vector3(direction.x * 0.5f, 0, 0);
        
        // İlk önce online ve offline modunu kontrol et
        bool isOfflineMode = GameManager.Instance != null && GameManager.Instance.isLocalHostMode;
        
        if (isOfflineMode || !NetworkManager.Singleton.IsListening)
        {
            // Offline modu için
            GameObject bullet = GameObject.Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            
            // Mermi yönünü ayarla
            Vector3 bulletScale = bullet.transform.localScale;
            bulletScale.x = direction.x >= 0 ? Mathf.Abs(bulletScale.x) : -Mathf.Abs(bulletScale.x);
            bullet.transform.localScale = bulletScale;
            
            // Mermi bileşenlerini ayarla
            EnemyBullets bulletScript = bullet.GetComponent<EnemyBullets>();
            
            // Mermi hızını ve yönünü ayarla
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * 10;
            }
            else if (bulletScript != null)
            {
                // EnemyBullet script'i kullanıyorsa
                bulletScript.moveDirection = direction;
            }
            
            // Mermi hasarını ayarla
            if (bulletScript != null)
            {
                bulletScript.stats = stats;
                bulletScript.bulletDamage = stats.enemyDamage;
            }
            
            // Belirli bir süre sonra mermiyi yok et
            GameObject.Destroy(bullet, 4f);
        }
        else
        {
            // Online mod için orijinal kodu kullan
            // Merminin yönünü hesapla
            bool isFlipped = direction.x < 0;
            
            // Mermiyi oluştur
            GameObject bullet = GameObject.Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            
            // Mermi bileşenlerini ayarla
            EnemyBullets bulletScript = bullet.GetComponent<EnemyBullets>();
            
            // Mermi hızını ayarla
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * 10;
            }
            
            // Mermi hasarını ayarla
            if (bulletScript != null)
            {
                bulletScript.stats = stats;
                bulletScript.bulletDamage = stats.enemyDamage;
            }
            
            // Mermi yönünü ayarla (flip)
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
            
            // Belirli bir süre sonra mermiyi yok et (eğer NetworkObject değilse)
            if (bullet.GetComponent<NetworkObject>() == null)
            {
                GameObject.Destroy(bullet, 4f);
            }
        }
    }
}
