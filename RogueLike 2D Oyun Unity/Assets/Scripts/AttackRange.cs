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
            Debug.LogError("AttackRange: Enemy GameObject could not be determined.");
            enabled = false;
            return;
        }
        
        if (stats == null) Debug.LogError($"EnemyStats not found on {enemy.name}!");
        if (animator == null) Debug.LogError($"Animator not found on {enemy.name}!");

        if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null) Debug.LogError("CircleCollider2D not found on AttackRange object!");
    }

    void Update()
    {
        if (!IsServer) return;
    }

    public void MoveTowardsPlayer(Transform targetPlayer)
    {
        if (!IsServer || targetPlayer == null || enemy == null || stats == null) return;

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
        if (!IsServer) return;

        if (collision.CompareTag("Player"))
        {
            if (!isAttacking)
            {
                KarakterHareket targetCharacter = collision.GetComponent<KarakterHareket>();
                if (targetCharacter != null)
                {
                    StartCoroutine(Attack(targetCharacter));
                }
            }
        }
    }

    private IEnumerator Attack(KarakterHareket targetCharacter)
    {
        isAttacking = true; 

        if (targetCharacter != null && targetCharacter.IsSpawned && stats != null && animator != null)
        {
            int damageToApply = stats.enemyDamage;
            if (damageToApply > 0)
            {
                Debug.Log($"[Server] AttackRange: Enemy ({enemy.name}) attacking Player {targetCharacter.OwnerClientId}. Damage: {damageToApply}");
                targetCharacter.ReceiveDamageServerRpc(damageToApply); 
            
                animator.SetTrigger("Attack");
            }
            else
            {
                Debug.LogWarning($"[Server] AttackRange: Enemy ({enemy.name}) attempting attack but damage is zero.");
            }
            
            float attackCooldown = (stats.attackSpeed > 0) ? (1.0f / stats.attackSpeed) : 1.0f;
            yield return new WaitForSeconds(attackCooldown);
        }
        else
        {
            Debug.LogWarning($"[Server] AttackRange: Attack coroutine stopped early. Target valid: {targetCharacter != null && targetCharacter.IsSpawned}, Stats valid: {stats != null}, Animator valid: {animator != null}");
        }

        isAttacking = false; 
    }

    public void Flip()
    {
        if (enemy == null) return;

        isFacingRight = !isFacingRight;
        Vector3 localScale = enemy.transform.localScale;
        localScale.x *= -1;
        enemy.transform.localScale = localScale;
        
        Transform canvasTransform = enemy.transform.Find("Canvas"); 
        if (canvasTransform != null)
        {
            Vector3 canvasScale = canvasTransform.localScale;
            canvasScale.x = Mathf.Abs(canvasScale.x) * Mathf.Sign(enemy.transform.localScale.x);
            canvasTransform.localScale = canvasScale;
        }
    }
}
