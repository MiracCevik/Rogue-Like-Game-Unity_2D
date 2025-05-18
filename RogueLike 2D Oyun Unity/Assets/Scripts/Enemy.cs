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
    private float attackTimer = 0f;
    private float detectionRange = 15f;

    private bool IsOfflineMode()
    {
        
            if (GameManager.Instance == null)
            {
                GameManager managerInScene = GameObject.FindObjectOfType<GameManager>();
                if (managerInScene != null)
                {
                    return managerInScene.isLocalHostMode;
                }
                return false;
            }
            
            return GameManager.Instance.isLocalHostMode;
        
      
    }

    void Awake()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            InitializeEnemy();
            
            if (enemyStats != null)
            {
                currentHealth = enemyStats.enemyHealth;
                currentDamage = enemyStats.enemyDamage;
                currentAS = enemyStats.attackSpeed;
            }
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
        FindPlayer();
        if (attackRange == null)
        {
            attackRange = GetComponent<AttackRange>();
        }

        if (enemyStats != null)
        {
            SaveEnemyData();
            LoadEnemyData();
        }
        
        UpdateHealthBar();
        AssignAttackBehavior();
    }

    void Update()
    {
        
            bool offline = false;
            try {
                offline = IsOfflineMode();
            }
            catch (System.Exception e) {
                offline = false;
            }

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || offline)
            {
                    UpdateEnemyBehavior();
             
                return;
            }  
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player != null)
            {
                karakterRef = player.GetComponent<KarakterHareket>();
            }
        }
    }

    private void UpdateEnemyBehavior()
    {
            FindPlayer();
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (player == null)
                {
                    return;
                }
            }

            if (enemyStats == null)
            {
                InitializeEnemy();
                if (enemyStats == null)
                {
                    return;
                }
            }

            if (attackBehavior == null)
            {
                AssignAttackBehavior();
              
            }

            float distance = Vector2.Distance(transform.position, player.position);
            
            UpdateEnemyFacing();
            if (distance <= detectionRange)
            {
               
                if (enemyStats.attackType == AttackType.Melee)
                {
                   
                        HandleMeleeEnemyBehavior(distance);
                   
                }
                else if (enemyStats.attackType == AttackType.Ranged)
                {
                    
                        HandleRangedEnemyBehavior(distance);
                   
                }
            }

            attackTimer += Time.deltaTime;
        
    }

    private void UpdateEnemyFacing()
    {
        if (player == null) return;
        bool shouldFaceRight = player.position.x > transform.position.x;
        bool isCurrentlyFacingRight = transform.localScale.x > 0;
        
        if (shouldFaceRight != isCurrentlyFacingRight)
        {
            Vector3 newScale = transform.localScale;
            newScale.x *= -1; 
            transform.localScale = newScale;
        }
    }

    private void HandleMeleeEnemyBehavior(float distance)
    {
        if (distance <= enemyStats.attackRange)
        {
            if (attackTimer >= 1f / enemyStats.attackSpeed && attackBehavior != null)
            {
                attackBehavior.ExecuteAttack(animator, player, enemyStats, transform, bulletPrefab);
                attackTimer = 0f;
            }
            if (distance < 0.8f)
            {
                Vector2 direction = (transform.position - player.position).normalized;
                transform.Translate(direction * enemyStats.moveSpeed * 0.5f * Time.deltaTime);
            }
            else
            {
                if (attackRange != null)
                {
                    attackRange.MoveTowardsPlayer(player);
                }
                else
                {
                    Vector2 direction = (player.position - transform.position).normalized;
                    transform.Translate(direction * enemyStats.moveSpeed * Time.deltaTime);
                }
            }
        }
        else if (distance <= enemyStats.attackRange * 3)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.Translate(direction * enemyStats.moveSpeed * Time.deltaTime);
        }
    }

    private void HandleRangedEnemyBehavior(float distance)
    {
        if (distance <= enemyStats.attackRange)
        {
            if (attackTimer >= 1f / enemyStats.attackSpeed && attackBehavior != null)
            {
                attackBehavior.ExecuteAttack(animator, player, enemyStats, transform, bulletPrefab);
                attackTimer = 0f;
            }
            
            if (distance < enemyStats.attackRange * 0.5f)
            {
                Vector2 direction = (transform.position - player.position).normalized;
                transform.Translate(direction * enemyStats.moveSpeed * 0.7f * Time.deltaTime);
            }
        }
        else if (distance > enemyStats.attackRange && distance <= enemyStats.attackRange * 2)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.Translate(direction * enemyStats.moveSpeed * Time.deltaTime);
        }
    }


    public void AssignAttackBehavior()
    {
        if (enemyStats == null) return;
        
        if (player == null || karakterRef == null)
        {
            FindPlayer();
            if (player == null || karakterRef == null) return;
        }
        
        switch (enemyStats.attackType)
        {
            case AttackType.Melee:
                attackBehavior = new MeleeAttack(karakterRef);
                break;
            case AttackType.Ranged:
                attackBehavior = new RangedAttack(karakterRef);
                break;
        }
    }

    private void InitializeEnemy()
    {
            if (enemyStats == null)
            {
                Object[] allStats = Resources.LoadAll("EnemyStats", typeof(EnemyStats));
                if (allStats != null && allStats.Length > 0)
                {
                    string prefabName = gameObject.name.Replace("(Clone)", "").Trim();
                    foreach (var stat in allStats)
                    {
                        EnemyStats loadedStat = stat as EnemyStats;
                        if (loadedStat.enemyName.Equals(prefabName, System.StringComparison.OrdinalIgnoreCase))
                        {
                            enemyStats = Instantiate(loadedStat);
                            break;
                        }
                    }
                }
                
                if (enemyStats == null)
                {
                    enemyStats = ScriptableObject.CreateInstance<EnemyStats>();
                    string prefabName = gameObject.name.Replace("(Clone)", "").Trim();
                    enemyStats.enemyName = prefabName;
                    
                    enemyStats.enemyDamage = 20;
                    enemyStats.enemyHealth = 200;
                    enemyStats.attackSpeed = 1.0f;
                    enemyStats.attackRange = 10f;
                    enemyStats.moveSpeed = 1.0f;
                }
            }

            if (enemyStats == null)
            {
                return;
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
                    
                    if (IsOfflineMode())
                    {
                        TakeDamage(weaponDamage);
                    }
                    else
                    {
                        TakeDamageServerRpc(weaponDamage);
                    }
                }
            }

        }
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, enemyStats.enemyHealth);
        UpdateHealthBar();

        if (currentHealth > 0)
        {
            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
            StartCoroutine(KnockbackCoroutine());
        }
        else
        {
            Die();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        TakeDamage(damage);
        networkHealth.Value = currentHealth;
    }

    private IEnumerator KnockbackCoroutine()
    {
        if (player == null) yield break;
        
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

    public void UpdateHealthBar()
    {
        if (greenHealthBar != null && redHealthBar != null)
        {
            greenHealthBar.gameObject.SetActive(true);
            redHealthBar.gameObject.SetActive(true);
            float healthRatio = (float)currentHealth / enemyStats.enemyHealth;
            greenHealthBar.rectTransform.localScale = new Vector3(healthRatio, 1, 1);
            redHealthBar.enabled = currentHealth < enemyStats.enemyHealth;
            Canvas canvas = greenHealthBar.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
                canvas.transform.rotation = Quaternion.identity;
            }
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
