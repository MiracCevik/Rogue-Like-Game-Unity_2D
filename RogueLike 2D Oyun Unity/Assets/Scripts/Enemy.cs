using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using Unity.Netcode;

public class Enemies : NetworkBehaviour
{
    public EnemyStats enemyStats;
    private IEnemyAttack attackBehavior;
    private NetworkVariable<int> networkHealth = new NetworkVariable<int>();
    private NetworkVariable<int> networkDamage = new NetworkVariable<int>();
    private NetworkVariable<float> networkAttackSpeed = new NetworkVariable<float>();
    public int currentHealth;
    public int currentDamage;
    public float currentAS;

    public GameObject bulletPrefab;

    public Image greenHealthBar;
    public Image redHealthBar;

    public Transform player;
    public Animator animator;
    public KarakterHareket karakterRef;
    public AttackRange attackRange;


    private string fileName = "EnemyStats.json";
    private bool isShooting;

    private bool IsOfflineMode()
    {
        return GameManager.Instance != null && GameManager.Instance.isLocalHostMode;
    }

    void Awake()
    {
        if (!NetworkManager.Singleton.IsListening)
        {
            InitializeEnemy();
            currentHealth = enemyStats.enemyHealth;
            currentDamage = enemyStats.enemyDamage;
            currentAS = enemyStats.attackSpeed;
            gameObject.SetActive(true);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            if (IsServer)
            {
                InitializeEnemy();
                networkHealth.Value = currentHealth;
                networkDamage.Value = currentDamage;
                networkAttackSpeed.Value = currentAS;
            }
            else
            {
                currentHealth = networkHealth.Value;
                currentDamage = networkDamage.Value;
                currentAS = networkAttackSpeed.Value;
            }
        }
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            karakterRef = player.GetComponent<KarakterHareket>();
           
        }
      
        if (attackRange == null)
        {
            attackRange = GetComponent<AttackRange>();
        }

        SaveEnemyData();
        LoadEnemyData();
        UpdateHealthBar();
        AssignAttackBehavior();
    }

    void Update()
    {
        if (!NetworkManager.Singleton.IsListening || IsOfflineMode())
        {
            UpdateEnemyBehavior();
            return;
        }

        if (!IsServer) return;
        UpdateEnemyBehavior();
    }

    private void UpdateEnemyBehavior()
    {
        if (player == null) 
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if(player == null) return; 
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (enemyStats.attackType == AttackType.Melee && distance <= enemyStats.attackRange)
        {
            if (attackRange != null)
            {
                 attackRange.MoveTowardsPlayer(player); 
            }
        }
        else if (enemyStats.attackType == AttackType.Ranged)
        {
            if (distance <= enemyStats.attackRange && !isShooting)
            {
                isShooting = true;
                StartCoroutine(FireRepeatedly());
            }

            if (distance > enemyStats.attackRange && isShooting)
            {
                isShooting = false;
                StopCoroutine(FireRepeatedly());
            }
        }
    }

    private IEnumerator FireRepeatedly()
    {
        while (isShooting)
        {
            attackBehavior.ExecuteAttack(animator, player, enemyStats, transform, bulletPrefab);
            yield return new WaitForSeconds(3f);
        }
    }
    private void AssignAttackBehavior()
    {
        switch (enemyStats.attackType)
        {
            case AttackType.Melee:
                attackBehavior = new MeleeAttack(karakterRef);
                attackBehavior.ExecuteAttack(animator, player, enemyStats, transform, bulletPrefab);
                break;
            case AttackType.Ranged:
                attackBehavior = new RangedAttack(karakterRef);
                attackBehavior.ExecuteAttack(animator, player, enemyStats, transform, bulletPrefab);
                break;
                
        }
    }

    private void InitializeEnemy()
    {
        if (enemyStats == null)
        {

            enemyStats = ScriptableObject.CreateInstance<EnemyStats>();
            enemyStats.enemyName = "Rock";
            enemyStats.enemyDamage = 20;
            enemyStats.enemyHealth = 200;
            enemyStats.attackSpeed = 1.0f;
            enemyStats.attackRange = 10f;
            enemyStats.moveSpeed = 1.0f;
        }

        currentHealth = enemyStats.enemyHealth;
        currentDamage = enemyStats.enemyDamage;
        currentAS = enemyStats.attackSpeed;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && enemyStats.enemyIcon != null)
        {
            spriteRenderer.sprite = enemyStats.enemyIcon;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsOfflineMode() || IsServer)
        {
            WeaponsScript weaponScript = collision.GetComponent<WeaponsScript>();
            if (collision.CompareTag("Weapon"))
            {
                if (weaponScript != null)
                {
                    int weaponDamage = weaponScript.weaponData != null ? weaponScript.weaponData.damage : 0;
                    TakeDamageServerRpc(weaponDamage);
                }
            }

            if (collision.CompareTag("Player"))
            {
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, enemyStats.enemyHealth);
        networkHealth.Value = currentHealth;
        UpdateHealthBar();

        if (currentHealth > 0)
        {
            animator.SetTrigger("Hit");
            StartCoroutine(KnockbackCoroutine());
        }
        else
        {
            Die();
        }
    }

    private IEnumerator KnockbackCoroutine()
    {
        Vector2 knockbackDirection = (transform.position - player.position).normalized;
        float knockbackForce = 5f;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(0.2f);
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    void UpdateHealthBar()
    {
        if (greenHealthBar != null && redHealthBar != null)
        {
            float healthRatio = (float)currentHealth / enemyStats.enemyHealth;
            greenHealthBar.rectTransform.localScale = new Vector3(healthRatio, 1, 1);
            redHealthBar.enabled = currentHealth < enemyStats.enemyHealth;
        }
    }

    void Die()
    {
        if (IsOfflineMode() || IsServer)
        {
            
            if (IsServer && gameObject != null && gameObject.GetComponent<NetworkObject>() != null)
            {
                GetComponent<NetworkObject>().Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
    public void SaveEnemyData()
    {
        if (enemyStats == null) return;

        EnemyData data = new EnemyData
        {
            enemyName = enemyStats.enemyName,
            enemyHealth = enemyStats.enemyHealth,
            enemyDamage = enemyStats.enemyDamage,
            attackSpeed = enemyStats.attackSpeed,
            attackRange = enemyStats.attackRange,
            moveSpeed = enemyStats.moveSpeed,
            currentHealth = currentHealth
        };

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(Application.persistentDataPath + "/" + fileName, json);
    }

    public void LoadEnemyData()
    {
        string path = Application.persistentDataPath + "/" + fileName;

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            EnemyData data = JsonUtility.FromJson<EnemyData>(json);

            if (enemyStats == null)
            {
                enemyStats = ScriptableObject.CreateInstance<EnemyStats>();
            }

            enemyStats.enemyName = data.enemyName;
            enemyStats.enemyHealth = data.enemyHealth;
            enemyStats.enemyDamage = data.enemyDamage;
            enemyStats.attackSpeed = data.attackSpeed;
            enemyStats.attackRange = data.attackRange;
            enemyStats.moveSpeed = data.moveSpeed;
            currentHealth = data.currentHealth;
        }
    }
}

[System.Serializable]
public class EnemyData
{
    public string enemyName;
    public int enemyHealth;
    public int enemyDamage;
    public float attackSpeed;
    public float attackRange;
    public float moveSpeed;
    public int currentHealth;
}
