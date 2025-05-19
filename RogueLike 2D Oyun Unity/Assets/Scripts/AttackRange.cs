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
    private KarakterHareket playerController;

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
        
        FindPlayer();
        if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();
    }

    void Update()
    {
        if (!IsOfflineMode() && !IsServer) return;
        
        if (player == null)
        {
            FindPlayer();
        }
    }

    private void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            playerController = player.GetComponent<KarakterHareket>();
        }
    }

    public void MoveTowardsPlayer(Transform targetPlayer)
    {
        if (targetPlayer == null)
        {
            FindPlayer();
            if (player == null) return;
            targetPlayer = player;
        }

        if ((!IsOfflineMode() && !IsServer) || enemy == null || stats == null) return;

        float distance = Vector2.Distance(enemy.transform.position, targetPlayer.position);
        bool shouldFaceRight = targetPlayer.position.x > enemy.transform.position.x;
        bool isCurrentlyFacingRight = enemy.transform.localScale.x > 0;
        
        if (shouldFaceRight != isCurrentlyFacingRight)
        {        
            Flip();
        }

        if (distance > 1.1f) 
        {
            Vector2 direction = (targetPlayer.position - enemy.transform.position).normalized;
            enemy.transform.Translate(direction * stats.moveSpeed * Time.deltaTime);
        }
        else if (distance < 0.8f)
        {
            Vector2 direction = (enemy.transform.position - targetPlayer.position).normalized;
            enemy.transform.Translate(direction * stats.moveSpeed * 0.5f * Time.deltaTime);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {   
        if (!IsOfflineMode() && !IsServer) return;
        
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
        if (collision.CompareTag("Player"))
        {
            isAttacking = false;
        }
    }

    private IEnumerator AttackPlayer(KarakterHareket player)
    {
        float attackInterval = stats != null ? 1f / stats.attackSpeed : 1f;
        
        while (isAttacking)
        {
            if (animator != null && stats != null && !string.IsNullOrEmpty(stats.normalAttackTrigger))
            {
                animator.SetTrigger(stats.normalAttackTrigger);
            }
            else if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            
            int damage = stats != null ? stats.enemyDamage : 10;
            player.TakeDamage(damage);
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
