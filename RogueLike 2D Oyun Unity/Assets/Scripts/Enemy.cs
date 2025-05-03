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

    void Awake()
    {
        // �evrimd��� modda d��man� ba�lat
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
        // �evrimi�i mod i�in
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
        // Oyuncuyu bul ve referanslar� ayarla
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            karakterRef = player.GetComponent<KarakterHareket>();
            if (karakterRef == null)
            {
                Debug.LogWarning("KarakterHareket bile�eni bulunamad�!");
            }
        }
        else
        {
            Debug.LogWarning("Player bulunamad�!");
        }

        // AttackRange bile�enini al
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
        // �evrimd��� mod i�in kontrol
        if (!NetworkManager.Singleton.IsListening)
        {
            UpdateEnemyBehavior();
            return;
        }

        // �evrimi�i mod i�in kontrol
        if (!IsServer) return;
        UpdateEnemyBehavior();
    }

    private void UpdateEnemyBehavior()
    {
        if (player == null) 
        {
            // Attempt to find player again if null (simple fallback, not ideal)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if(player == null) return; // Still no player found, can't proceed
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (enemyStats.attackType == AttackType.Melee && distance <= enemyStats.attackRange)
        {
            // Pass the player transform to MoveTowardsPlayer
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
                /* case AttackType.Boss:
                     attackBehavior = new BossAttack();
                     break;*/
        }
    }

    private void InitializeEnemy()
    {
        if (enemyStats == null)
        {
            Debug.LogWarning("EnemyStats atanmad�, varsay�lan de�erler kullan�l�yor.");

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
        if (!IsServer) return;

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
            // animator.SetTrigger(enemyStats.normalAttackTrigger);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        currentHealth -= damage;
        Debug.Log("damage: " + damage);
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
            Debug.Log("enemy can�  " + currentHealth);
            redHealthBar.enabled = currentHealth < enemyStats.enemyHealth;
        }
    }

    void Die()
    {
        if (!IsServer) return;

        karakterRef.gold += enemyStats.rewards;
        Debug.Log("Karakter Alt�n�: " + karakterRef.gold);
        SkillTreeManager skillTreeManager = FindObjectOfType<SkillTreeManager>();
        if (skillTreeManager != null)
        {
            skillTreeManager.UpdateGoldUI(karakterRef.gold);
        }

        SaveManager.Instance.SaveGame();
        NetworkObject networkObject = GetComponent<NetworkObject>();
        networkObject.Despawn();
    }

    public void SaveEnemyData()
    {
        if (enemyStats != null)
        {
            string json = JsonUtility.ToJson(enemyStats, true);
            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(path, json);
        }
        else
        {
            Debug.LogError("Kaydedilecek EnemyStats bulunamad�!");
        }
    }

    void LoadEnemyData()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(json, enemyStats);
        }
        else
        {
            Debug.LogError("D��man verisi bulunamad�, varsay�lan de�erler y�klenecek.");
        }
    }

}
