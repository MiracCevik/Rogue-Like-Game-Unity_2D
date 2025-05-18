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
    private float attackTimer = 0f;
    private float detectionRange = 15f; // Düşmanların oyuncuyu algılama menzili

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
        FindPlayer();
        
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
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        
        // Düşman yönünü oyuncuya doğru çevir (flip)
        UpdateEnemyFacing();

        // Düşman görüş menzili içinde mi kontrol et
        if (distance <= detectionRange)
        {
            // Melee düşman davranışı
            if (enemyStats.attackType == AttackType.Melee)
            {
                HandleMeleeEnemyBehavior(distance);
            }
            // Ranged düşman davranışı
            else if (enemyStats.attackType == AttackType.Ranged)
            {
                HandleRangedEnemyBehavior(distance);
            }
        }

        // Saldırı zamanı ayarlaması (cooldown)
        attackTimer += Time.deltaTime;
    }

    private void UpdateEnemyFacing()
    {
        if (player == null) return;
        
        // Düşmanın yönünü oyuncuya göre ayarla
        bool shouldFaceRight = player.position.x > transform.position.x;
        bool isCurrentlyFacingRight = transform.localScale.x > 0;

        if (shouldFaceRight != isCurrentlyFacingRight)
        {
            Vector3 newScale = transform.localScale;
            newScale.x *= -1; // Sadece X ekseninde çevir, boyutu değiştirmeden
            transform.localScale = newScale;
        }
    }

    private void HandleMeleeEnemyBehavior(float distance)
    {
        // Saldırı mesafesi içindeyse
        if (distance <= enemyStats.attackRange)
        {
            // Saldırı cooldown'ı dolduysa saldır
            if (attackTimer >= 1f / enemyStats.attackSpeed)
            {
                attackBehavior.ExecuteAttack(animator, player, enemyStats, transform, bulletPrefab);
                attackTimer = 0f;
            }
            
            // Çok yakınsa biraz mesafe al
            if (distance < 0.8f)
            {
                Vector2 direction = (transform.position - player.position).normalized;
                transform.Translate(direction * enemyStats.moveSpeed * 0.5f * Time.deltaTime);
            }
            // Aksi halde yaklaş
            else
            {
                if (attackRange != null)
                {
                    attackRange.MoveTowardsPlayer(player);
                }
                else
                {
                    // AttackRange yoksa, düşmanı doğrudan oyuncuya doğru hareket ettir
                    Vector2 direction = (player.position - transform.position).normalized;
                    transform.Translate(direction * enemyStats.moveSpeed * Time.deltaTime);
                }
            }
        }
        // Görüş menzili içindeyse ama saldırı menzilinde değilse, oyuncuya doğru git
        else if (distance <= enemyStats.attackRange * 3)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.Translate(direction * enemyStats.moveSpeed * Time.deltaTime);
        }
    }

    private void HandleRangedEnemyBehavior(float distance)
    {
        // Menzil içindeyse ateş et
        if (distance <= enemyStats.attackRange)
        {
            // Ateş etme cooldown'ı dolduysa ateş et
            if (attackTimer >= 1f / enemyStats.attackSpeed)
            {
                attackBehavior.ExecuteAttack(animator, player, enemyStats, transform, bulletPrefab);
                attackTimer = 0f;
            }
            
            // Eğer oyuncu çok yakındaysa, uzaklaş
            if (distance < enemyStats.attackRange * 0.5f)
            {
                Vector2 direction = (transform.position - player.position).normalized;
                transform.Translate(direction * enemyStats.moveSpeed * 0.7f * Time.deltaTime);
            }
        }
        // Menzil dışındaysa ama görüş içindeyse, yaklaş
        else if (distance > enemyStats.attackRange && distance <= enemyStats.attackRange * 2)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.Translate(direction * enemyStats.moveSpeed * Time.deltaTime);
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

    public void AssignAttackBehavior()
    {
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
                    
                    if (IsOfflineMode())
                    {
                        // Offline modda doğrudan hasar uygula
                        TakeDamage(weaponDamage);
                    }
                    else
                    {
                        // Online modda RPC kullan
                        TakeDamageServerRpc(weaponDamage);
                    }
                }
            }

            if (collision.CompareTag("Player"))
            {
            }
        }
    }
    
    // Offline mod için hasar alma metodu
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
        // Sunucu tarafında hasar uygulanıyor
        TakeDamage(damage);
        
        // Network değişkenini güncelle
        networkHealth.Value = currentHealth;
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

    public void UpdateHealthBar()
    {
        if (greenHealthBar != null && redHealthBar != null)
        {
            // Make sure health bars are active
            greenHealthBar.gameObject.SetActive(true);
            redHealthBar.gameObject.SetActive(true);
            
            // Update health bar scale based on current health
            float healthRatio = (float)currentHealth / enemyStats.enemyHealth;
            greenHealthBar.rectTransform.localScale = new Vector3(healthRatio, 1, 1);
            redHealthBar.enabled = currentHealth < enemyStats.enemyHealth;
            
            // Make sure health bar is above the enemy
            Canvas canvas = greenHealthBar.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
                canvas.transform.rotation = Quaternion.identity; // Keep health bar facing the camera
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
