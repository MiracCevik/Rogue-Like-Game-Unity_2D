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
        try
        {
            // GameManager.Instance null kontrolü
            if (GameManager.Instance == null)
            {
                // GameManager'ı bulmaya çalış
                GameManager managerInScene = GameObject.FindObjectOfType<GameManager>();
                if (managerInScene != null)
                {
                    return managerInScene.isLocalHostMode;
                }
                return false;
            }
            
            return GameManager.Instance.isLocalHostMode;
        }
        catch (System.Exception e)
        {
            Debug.LogError("IsOfflineMode detaylı hata: " + e.Message);
            return false;
        }
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
        // Oyuncuyu bul
        FindPlayer();
        
        // AttackRange bileşenini al (yoksa)
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
        try
        {
            // NetworkManager veya GameManager kontrolü için daha güvenli kontroller
            bool offline = false;
            try {
                offline = IsOfflineMode();
            }
            catch (System.Exception e) {
                Debug.LogWarning("IsOfflineMode hata: " + e.Message);
                offline = false;
            }

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || offline)
            {
                // İç içe try-catch ile hata yerini daha iyi belirle
                try {
                    UpdateEnemyBehavior();
                }
                catch (System.Exception e) {
                    Debug.LogError("UpdateEnemyBehavior hata: " + e.Message + "\n" + e.StackTrace);
                }
                return;
            }

            if (!IsServer) return;
            
            try {
                UpdateEnemyBehavior();
            }
            catch (System.Exception e) {
                Debug.LogError("UpdateEnemyBehavior (Server) hata: " + e.Message + "\n" + e.StackTrace);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Enemy Update ana hata: " + ex.Message + "\n" + ex.StackTrace);
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
        try
        {
            // Oyuncu kontrolü
            FindPlayer();
            if (player == null)
            {
                // Son bir kez daha dene
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (player == null)
                {
                    Debug.LogWarning("Düşman, oyuncuyu bulamadı");
                    return;
                }
            }

            // Enemy Stats kontrolü
            if (enemyStats == null)
            {
                // Enemy Stats yoksa yeniden oluştur
                InitializeEnemy();
                if (enemyStats == null)
                {
                    Debug.LogWarning("Enemy Stats bulunamadı veya oluşturulamadı");
                    return;
                }
            }

            // Attack Behavior kontrolü
            if (attackBehavior == null)
            {
                AssignAttackBehavior();
                if (attackBehavior == null)
                {
                    Debug.LogWarning("Attack Behavior bulunamadı veya oluşturulamadı");
                    // Devam et, en azından hareket edebilir
                }
            }

            float distance = Vector2.Distance(transform.position, player.position);
            
            // Düşman yönünü oyuncuya doğru çevir (flip)
            UpdateEnemyFacing();

            // Düşman görüş menzili içinde mi kontrol et
            if (distance <= detectionRange)
            {
                // Düşman tipine göre davranış belirle
                if (enemyStats.attackType == AttackType.Melee)
                {
                    try
                    {
                        HandleMeleeEnemyBehavior(distance);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Melee behavior hata: " + e.Message);
                    }
                }
                else if (enemyStats.attackType == AttackType.Ranged)
                {
                    try
                    {
                        HandleRangedEnemyBehavior(distance);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Ranged behavior hata: " + e.Message);
                    }
                }
            }

            // Saldırı zamanı ayarlaması (cooldown)
            attackTimer += Time.deltaTime;
        }
        catch (System.Exception e)
        {
            Debug.LogError("UpdateEnemyBehavior ana hata: " + e.Message + "\n" + e.StackTrace);
        }
    }

    private void UpdateEnemyFacing()
    {
        if (player == null) return;
        
        // Düşmanın yönünü oyuncuya göre ayarla - Düzeltilmiş yön mantığı
        // Eğer oyuncu düşmanın sağındaysa, düşman sağa bakmalı (pozitif scale)
        // Eğer oyuncu düşmanın solundaysa, düşman sola bakmalı (negatif scale)
        bool shouldFaceRight = player.position.x > transform.position.x;
        
        // Düşmanın şu anda hangi yöne baktığını kontrol et
        bool isCurrentlyFacingRight = transform.localScale.x > 0;
        
        // Eğer bakması gereken yön ile şu anki yönü farklıysa, çevir
        if (shouldFaceRight != isCurrentlyFacingRight)
        {
            // Düşmanı çevir
            Vector3 newScale = transform.localScale;
            newScale.x *= -1; // Sadece X ekseninde çevir, boyutu değiştirmeden
            transform.localScale = newScale;
            
            // Debug log ekleyerek kontrol et
            Debug.Log($"Düşman yönü değiştirildi. Oyuncu pozisyonu X: {player.position.x}, Düşman pozisyonu X: {transform.position.x}, Sağa bakmalı: {shouldFaceRight}");
        }
    }

    private void HandleMeleeEnemyBehavior(float distance)
    {
        // Saldırı mesafesi içindeyse
        if (distance <= enemyStats.attackRange)
        {
            // Saldırı cooldown'ı dolduysa saldır
            if (attackTimer >= 1f / enemyStats.attackSpeed && attackBehavior != null)
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
            if (attackTimer >= 1f / enemyStats.attackSpeed && attackBehavior != null)
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
        // enemyStats veya karakterRef null ise çık
        if (enemyStats == null) return;
        
        // Eğer player ve karakterRef null ise oyuncuyu bulmaya çalış
        if (player == null || karakterRef == null)
        {
            FindPlayer();
            // Hala null ise çık
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
        try
        {
            if (enemyStats == null)
            {
                // Önce enemyStats proje kaynaklarından yüklemeyi dene
                Object[] allStats = Resources.LoadAll("EnemyStats", typeof(EnemyStats));
                if (allStats != null && allStats.Length > 0)
                {
                    // Prefab ismine göre uygun enemy stats seç
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
                
                // Hala bulunamadıysa yeni oluştur
                if (enemyStats == null)
                {
                    enemyStats = ScriptableObject.CreateInstance<EnemyStats>();
                    string prefabName = gameObject.name.Replace("(Clone)", "").Trim();
                    enemyStats.enemyName = prefabName;
                    
                    // Varsayılan değerler
                    enemyStats.enemyDamage = 20;
                    enemyStats.enemyHealth = 200;
                    enemyStats.attackSpeed = 1.0f;
                    enemyStats.attackRange = 10f;
                    enemyStats.moveSpeed = 1.0f;
                    
                    Debug.Log($"{gameObject.name} için yeni EnemyStats oluşturuldu");
                }
            }

            // Eğer hala null ise (olmamalı) hata ayıklama
            if (enemyStats == null)
            {
                Debug.LogError($"{gameObject.name} için EnemyStats oluşturulamadı!");
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
        catch (System.Exception e)
        {
            Debug.LogError("InitializeEnemy hata: " + e.Message + "\n" + e.StackTrace);
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
