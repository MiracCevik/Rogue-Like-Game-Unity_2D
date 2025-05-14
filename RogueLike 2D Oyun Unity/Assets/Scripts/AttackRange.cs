using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class AttackRange : NetworkBehaviour
{
    public Transform player;
    public GameObject enemy;
    public float moveSpeed = 1.0f;
    private float originalRadius;
    public CircleCollider2D circleCollider;
    public EnemyStats stats;
    private Animator animator;
    private bool isAttacking = false;
    private bool isFacingRight;

    // Offline (local host) modunda olup olmadığımızı kontrol et
    private bool IsOfflineMode()
    {
        return GameManager.Instance != null && GameManager.Instance.isLocalHostMode;
    }

    void Start()
    {
        if (enemy == null) enemy = transform.parent?.gameObject;
        if (enemy == null) enemy = gameObject;

        if (enemy != null)
        {
            stats = enemy.GetComponent<Enemies>()?.enemyStats;
            animator = enemy.GetComponent<Animator>();
        }
        else
        {
            enabled = false;
            return;
        }
        

        if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();
    }

    void Update()
    {
        // Offline modda veya server ise çalışsın
        if (!IsOfflineMode() && !IsServer) return;
    }

    public void MoveTowardsPlayer(Transform targetPlayer)
    {
        // Offline modda veya server ise düşman hareketlerini yönet
        if (!IsOfflineMode() && !IsServer || targetPlayer == null || enemy == null || stats == null) return;

        float distance = Vector2.Distance(enemy.transform.position, targetPlayer.position);

        bool shouldFaceRight = targetPlayer.position.x > enemy.transform.position.x;
        if ((shouldFaceRight && enemy.transform.localScale.x < 0) || (!shouldFaceRight && enemy.transform.localScale.x > 0))
        {        
            Flip();
        }

        if (distance > 1.1f) 
        {
            Vector2 direction = (targetPlayer.position - enemy.transform.position).normalized;
            enemy.transform.Translate(direction * stats.moveSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {   
        // Offline modda veya server ise çarpışma tespiti yapsın
        if (!IsOfflineMode() && !IsServer) return;
        
        // Eğer çarpışan nesne oyuncu ise ve saldırı durumunda değilsek
        if (collision.CompareTag("Player") && !isAttacking)
        {
            KarakterHareket player = collision.GetComponent<KarakterHareket>();
            if (player != null)
            {
                isAttacking = true;
                StartCoroutine(AttackPlayer(player));
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Oyuncu saldırı alanından çıkınca saldırıyı durdur
        if (collision.CompareTag("Player"))
        {
            isAttacking = false;
        }
    }

    private IEnumerator AttackPlayer(KarakterHareket player)
    {
        // Düşman saldırı aralığı
        float attackInterval = stats != null ? 1f / stats.attackSpeed : 1f;
        
        while (isAttacking)
        {
            // Animasyon tetikle
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            
            // Hasar ver
            int damage = stats != null ? stats.enemyDamage : 10;
            player.TakeDamage(damage);
            
            // Saldırı aralığı kadar bekle
            yield return new WaitForSeconds(attackInterval);
        }
    }

    private void Flip()
    {
        if (enemy != null)
        {
            Vector3 currentScale = enemy.transform.localScale;
            currentScale.x *= -1;
            enemy.transform.localScale = currentScale;
        }
    }
}
