using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;

public class KarakterHareket : NetworkBehaviour
{
    public float hiz = 1f;
    public float ziplama = 1f;
    public int maxJumpCount = 2;
    public int jumpCounter = 0;

    public int CharHealth;
    public int currentHealth;
    public int attackDamage;
    public int abilityPower;
    public int armor;
    public int gold;
    public string currentWeapon;
    public Image greenHealthBar;
    public Image redHealthBar;

    public Animator characterAnimator;
    private Animator weaponAnimator;
    private Rigidbody2D rb;
    public WeaponsScript weaponsScript;
    public ArmScript armScript;
    public DefaultCharacterStats defaultStats;
    public GameObject weaponObject;
    public float chargeTime = 0.5f;
    public static KarakterHareket instance;

    private bool canAttack = true;
    public float attackSpeed = 1.0f;
    private bool isTakingDamage;

    private NetworkVariable<int> networkHealth = new NetworkVariable<int>();
    private NetworkVariable<int> networkAttackDamage = new NetworkVariable<int>();
    private NetworkVariable<int> networkAbilityPower = new NetworkVariable<int>();
    private NetworkVariable<int> networkArmor = new NetworkVariable<int>();
    private NetworkVariable<int> networkGold = new NetworkVariable<int>();
    private NetworkVariable<int> networkLives = new NetworkVariable<int>(3, writePerm: NetworkVariableWritePermission.Server);
    private int offlineLives = 3;

    public GameObject AoePrefab;

    private Dictionary<ulong, Dictionary<int, float>> playerSkillCooldowns = new Dictionary<ulong, Dictionary<int, float>>();
    private Dictionary<int, float> skillLastUseTime = new Dictionary<int, float>();

    private TMP_Text lifeText;

    private bool IsOfflineMode()
    {
        bool result = GameManager.Instance != null && GameManager.Instance.isLocalHostMode;
        return result;
    }

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>(); 
        
        characterAnimator = GetComponent<Animator>();

        if (IsOfflineMode() || IsOwner)
        {
            enabled = true;
            Camera playerCamera = GetComponentInChildren<Camera>(true); 
            if (playerCamera != null) 
            { 
                playerCamera.gameObject.SetActive(true);
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            } 
            
            if (weaponObject != null) 
            { 
                weaponsScript = weaponObject.GetComponent<WeaponsScript>(); 
                weaponAnimator = weaponObject.GetComponent<Animator>();
            }
        }
        else 
        {   
            enabled = true;
            
            Camera playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera != null) 
            {
                 playerCamera.gameObject.SetActive(false); 
                 AudioListener listener = playerCamera.GetComponent<AudioListener>();
                 if (listener != null) listener.enabled = false;
            }
            
            if (!IsServer && rb != null) 
            { 
                rb.isKinematic = true; 
            }
        }

        networkHealth.OnValueChanged += OnHealthChanged;
        networkLives.OnValueChanged += OnLivesChanged;

        if (IsServer)
        {
            ResetToDefaultStats();
            networkHealth.Value = CharHealth;
            networkAttackDamage.Value = attackDamage;
            networkAbilityPower.Value = abilityPower;
            networkArmor.Value = armor;
            networkGold.Value = gold;
        }
        else
        {
            currentHealth = networkHealth.Value; 
        }
       
        UpdateHealthBar();
        if (lifeText == null && IsOwner)
        {
            var lifeObj = GameObject.Find("Life");
            if (lifeObj != null)
                lifeText = lifeObj.GetComponentInChildren<TMP_Text>();
        }
        UpdateLifeText(networkLives.Value);
    }

    void Awake()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                enabled = true;
                characterAnimator = GetComponent<Animator>();
                rb = GetComponent<Rigidbody2D>();
                if (weaponObject != null)
                {
                    weaponsScript = weaponObject.GetComponent<WeaponsScript>();
                    weaponAnimator = weaponObject.GetComponent<Animator>();
                }
                
                if (defaultStats != null)
                {
                    CharHealth = defaultStats.maxHealth;
                    currentHealth = defaultStats.maxHealth;
                    attackDamage = defaultStats.attackDamage;
                    abilityPower = defaultStats.abilityPower;
                    armor = defaultStats.armor;
                    
                }
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeLifeText();
    }

    void Start()
    {
        InitializeLifeText();
    }

    private void InitializeLifeText()
    {
        if (lifeText == null)
        {
            var lifeObj = GameObject.Find("Life");
            if (lifeObj != null)
            {
                lifeText = lifeObj.GetComponentInChildren<TMP_Text>();
                Debug.Log("lifeText başlatıldı: " + lifeText);
            }
            else
            {
                Debug.LogError("Life objesi bulunamadı!");
            }
        }
        UpdateLifeText(NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening ? networkLives.Value : 3);
    }

    void Update()
    {
        float hareketInputX = Input.GetAxis("Horizontal");
        bool jumpPressed = Input.GetButtonDown("Jump");
        
        if (!IsOfflineMode() && !IsOwner) 
        {
            return;
        }

        if (Mathf.Abs(hareketInputX) > 0.01f)
        {
            if (characterAnimator != null) characterAnimator.SetBool("isRunning", true);
        }
        else
        {
            if (characterAnimator != null) characterAnimator.SetBool("isRunning", false);
        }
        
        if (jumpPressed)
        {
            if (characterAnimator != null) characterAnimator.SetTrigger("isJumping");
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode())
        {
            if (rb != null)
            {
                rb.velocity = new Vector2(hareketInputX * hiz, rb.velocity.y);

                if (jumpPressed && jumpCounter < maxJumpCount)
                {
                    rb.velocity = new Vector2(rb.velocity.x, 0);
                    rb.AddForce(Vector2.up * ziplama, ForceMode2D.Impulse);
                    jumpCounter++;
                }
                
                if (hareketInputX > 0.01f)
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
                else if (hareketInputX < -0.01f)
                {
                    transform.localScale = new Vector3(-1, 1, 1);
                }
            }
        }
        else
        {
            SubmitMovementInputServerRpc(hareketInputX, jumpPressed);
        }

        HandleAttack(); 
        HandleSkillInput(); 
    }

    void HandleSkillInput()
    {
        bool isOfflineMode = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode();
        
        if (isOfflineMode)
        {
            if (Input.GetKeyDown(KeyCode.Q)) UseLocalSkill(0);
            if (Input.GetKeyDown(KeyCode.E)) UseLocalSkill(1);
            if (Input.GetKeyDown(KeyCode.Z)) UseLocalSkill(2);
            if (Input.GetKeyDown(KeyCode.C)) UseLocalSkill(3);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Q)) RequestUseSkillServerRpc(0);
            if (Input.GetKeyDown(KeyCode.E)) RequestUseSkillServerRpc(1);
            if (Input.GetKeyDown(KeyCode.Z)) RequestUseSkillServerRpc(2);
            if (Input.GetKeyDown(KeyCode.C)) RequestUseSkillServerRpc(3);
        }
    }
    
    private void UseLocalSkill(int skillIndex)
    {
        SkillTreeManager manager = FindObjectOfType<SkillTreeManager>();
        SkillData skill = manager.skills[skillIndex];
        
        if (!skill.isUnlocked)
        {
            Debug.Log($"Yetenek {skillIndex} ({skill.skillName}) açık değil.");
            return;
        }
        
        if (!skillLastUseTime.TryGetValue(skillIndex, out float lastUseTime))
        {
            lastUseTime = -skill.cooldown;
            skillLastUseTime[skillIndex] = lastUseTime;
        }
        
        if (Time.time < lastUseTime + skill.cooldown)
        {
            float remainingCooldown = lastUseTime + skill.cooldown - Time.time;
            Debug.Log($"Yetenek {skillIndex} ({skill.skillName}) bekleme süresinde: {remainingCooldown:F1} saniye kaldı.");
            return;
        }
        
        skillLastUseTime[skillIndex] = Time.time;
        
        if (manager.skillIcons.Count > skillIndex && manager.skillIcons[skillIndex] != null)
        {
            manager.skillIcons[skillIndex].StartCooldown(skill.cooldown);
        }
        
        switch (skill.skillType)
        {
            case SkillData.SkillType.ChargeAttack:
                StartCoroutine(LocalChargeAttack(skill));
                break;
            case SkillData.SkillType.AoEAttack:
                StartCoroutine(LocalAoEAttack(skill));
                break;
            case SkillData.SkillType.Dash:
                StartCoroutine(LocalDash(skill));
                break;
            case SkillData.SkillType.Heal:
                LocalHeal(skill);
                break;
            default:
                Debug.LogError($"Bilinmeyen yetenek tipi: {skill.skillType}");
                break;
        }
    }
    
    private IEnumerator LocalChargeAttack(SkillData skill)
    {
        if (weaponsScript == null || weaponsScript.weaponData == null)
        {
            yield break;
        }

        string animTrigger = weaponsScript.weaponData.normalAttackTrigger;
        if (string.IsNullOrEmpty(animTrigger))
        {
            switch (weaponsScript.weaponData.weaponName)
            {
                case "Sword":
                    animTrigger = "SwordAttack";
                    break;
                case "Sycthe":
                    animTrigger = "ScytheAttack";
                    break;
                case "Hammer":
                    animTrigger = "HammerAttack";
                    break;
                case "Bow":
                    animTrigger = "BowAttack";
                    break;
                default:
                    animTrigger = "Attack";
                    break;
            }
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger(animTrigger);
        }

        StopCoroutine(AttackCooldown());
        canAttack = true;

        string weaponName = weaponsScript.weaponData.weaponName;
        bool isRangedWeapon = weaponName == "Bow" || weaponName == "Pistol" || weaponName == "Rifle";

        if (isRangedWeapon)
        {
            GameObject bulletPrefab = null;
            
            if (weaponName == "Pistol" && weaponsScript.pistolPrefab != null)
            {
                bulletPrefab = weaponsScript.pistolBulletPrefab;
            }
            else if (weaponName == "Rifle" && weaponsScript.riflePrefab != null)
            {
                bulletPrefab = weaponsScript.rifleBulletPrefab;
            }
            else if (weaponName == "Bow")
            {
                bulletPrefab = weaponsScript.arrowPrefab;
            }

            if (bulletPrefab != null)
            {
                Vector3 spawnPosition = weaponObject.transform.position;
                Vector3 direction = new Vector3(transform.localScale.x, 0, 0).normalized;
                GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
                
                Bullet bulletComponent = bullet.GetComponent<Bullet>();
                if (bulletComponent != null)
                {
                    int skillDamage = (weaponsScript.weaponData.damage + attackDamage) * 2;
                    bulletComponent.Initialize(direction, skillDamage, gameObject);
                }
            }
        }
        else
        {
            float attackRange = 1.0f;
            float attackRadius = 0.5f;
            Vector2 attackOrigin = (Vector2)transform.position + (Vector2)(transform.right * transform.localScale.x * (attackRange * 0.5f));

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            LayerMask targetMask = 1 << enemyLayer;

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin, attackRadius, targetMask);

            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                Enemies enemyTarget = hit.GetComponent<Enemies>();
                if (enemyTarget != null)
                {
                    int weaponDamage = weaponsScript.weaponData.damage;
                    int totalDamage = (weaponDamage + attackDamage) * 2;
                    enemyTarget.TakeDamage(totalDamage);
                }
            }
        }

        yield return new WaitForSeconds(skill.cooldown);
    }

    private IEnumerator LocalAoEAttack(SkillData skill)
    {
        if (AoePrefab == null)
        {
            yield break;
        }
        
        GameObject aoeInstance = Instantiate(AoePrefab, transform.position, Quaternion.identity);
        
        int skillDamage = (weaponsScript != null && weaponsScript.weaponData != null) 
            ? (int)(weaponsScript.weaponData.damage * skill.damageMultiplier) 
            : (int)(attackDamage * skill.damageMultiplier);
        
        float aoeRadius = 5.0f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
        int hitCount = 0;
        
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            
            if (hit.CompareTag("Enemy"))
            {
                hitCount++;
                Enemies enemyTarget = hit.GetComponent<Enemies>();
                if (enemyTarget != null)
                {
                    enemyTarget.TakeDamage(skillDamage);
                }
                
            }
        }
        yield return new WaitForSeconds(skill.chargeTime);
        Destroy(aoeInstance);
    }

    private IEnumerator LocalDash(SkillData skill)
    {
        if (rb == null) yield break;
        
        float dashDirection = transform.localScale.x > 0 ? 1f : -1f;
        float dashDuration = 0.4f;
        int dashDamage = 15;
        float dashDistance = 5f;

        int originalLayer = gameObject.layer;
        gameObject.layer = LayerMask.NameToLayer("Invulnerable");

        Vector2 originalVelocity = rb.velocity;
        rb.velocity = Vector2.zero;
        Vector2 targetPosition = (Vector2)transform.position + Vector2.right * dashDirection * dashDistance;

        float elapsedTime = 0f;
        Vector2 startPosition = transform.position;
        int totalHitCount = 0;
        
        
        while (elapsedTime < dashDuration)
        {
            float t = elapsedTime / dashDuration;
            t = Mathf.SmoothStep(0, 1, t);
            transform.position = Vector2.Lerp(startPosition, targetPosition, t);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.2f);
            
            foreach(var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                
                if (hit.CompareTag("Enemy"))
                {
                    Enemies enemy = hit.GetComponent<Enemies>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(dashDamage);
                        
                        totalHitCount++;
                    }
                  
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        gameObject.layer = originalLayer;
        rb.velocity = originalVelocity;
    }

    private void LocalHeal(SkillData skill)
    {
        int healAmount = Mathf.Max(20, (int)skill.damageMultiplier);
        
        currentHealth = Mathf.Clamp(currentHealth + 300, 0, CharHealth);
        UpdateHealthBar();
    }

    [ServerRpc]
    void RequestUseSkillServerRpc(int skillIndex, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        SkillTreeManager manager = FindObjectOfType<SkillTreeManager>();
        SkillData skill = manager.skills[skillIndex];

        if (!playerSkillCooldowns.TryGetValue(clientId, out Dictionary<int, float> playerCooldowns))
        {
            playerCooldowns = new Dictionary<int, float>();
            playerSkillCooldowns[clientId] = playerCooldowns;
        }
        if (playerCooldowns.TryGetValue(skillIndex, out float lastUseTime))
        {
            float timeSinceLastUse = Time.time - lastUseTime;
            if (timeSinceLastUse < skill.cooldown)
            {
                float remainingCooldown = skill.cooldown - timeSinceLastUse;
                Debug.Log($"[Server] Client {clientId} trying to use skill {skillIndex} but on cooldown: {remainingCooldown:F1}s remaining");
                return;
            }
        }

        playerCooldowns[skillIndex] = Time.time;

        switch (skill.skillType)
        {
            case SkillData.SkillType.ChargeAttack:
                StartCoroutine(ChargeAttackServer(skill));
                break;
            case SkillData.SkillType.AoEAttack:
                 StartCoroutine(AoEAttackServer(skill));
                break;
            case SkillData.SkillType.Dash:
                 StartCoroutine(DashServer(skill));
                break;
            case SkillData.SkillType.Heal:
                 HealServer(skill);
                break;
            default:
                Debug.LogError($"[Server - {clientId}] Unknown SkillType: {skill.skillType}");
                break;
        }

        TriggerSkillCooldownClientRpc(skillIndex, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });
    }

    [ServerRpc]
    void SubmitMovementInputServerRpc(float hareketInputX, bool jumpPressed)
    {
        if (rb != null)
        {
             rb.velocity = new Vector2(hareketInputX * hiz, rb.velocity.y);

             if (jumpPressed && jumpCounter < maxJumpCount)
             {
                 rb.velocity = new Vector2(rb.velocity.x, 0);
                 rb.AddForce(Vector2.up * ziplama, ForceMode2D.Impulse);
                 jumpCounter++;
             }
            
        }

        if (hareketInputX > 0.01f)
        {
            FlipCharacterClientRpc(1);
        }
        else if (hareketInputX < -0.01f)
        {
            FlipCharacterClientRpc(-1);
        }
    }

    [ClientRpc]
    void FlipCharacterClientRpc(int direction)
    {
        if (transform != null)
        {
            transform.localScale = new Vector3(direction, 1, 1);
        }
    }

    [ServerRpc]
    public void AttackServerRpc(ServerRpcParams rpcParams = default)
    {
        if (weaponObject != null)
        {
            weaponAnimator = weaponObject.GetComponent<Animator>();
          
        }
     
        var clientId = rpcParams.Receive.SenderClientId;


        string animTrigger = weaponsScript.weaponData.normalAttackTrigger;
        if (string.IsNullOrEmpty(animTrigger))
        {
            switch (weaponsScript.weaponData.weaponName)
            {
                case "Sword":
                    animTrigger = "SwordAttack";
                    break;
                case "Sycthe":
                    animTrigger = "ScytheAttack";
                    break;
                case "Hammer":
                    animTrigger = "HammerAttack";
                    break;
                case "Bow":
                    animTrigger = "BowAttack";
                    break;
                default:
                    animTrigger = "Attack";
                    break;
            }
        }

        TriggerAttackAnimationClientRpc(animTrigger);

        canAttack = false;
        StartCoroutine(AttackCooldown());

        string weaponName = weaponsScript.weaponData.weaponName;
        bool isRangedWeapon = weaponName == "Bow" || weaponName == "Pistol" || weaponName == "Rifle";
        
        if (isRangedWeapon)
        {
            HandleRangedWeaponAttack(weaponName, clientId);
        }
        else
        {
            float attackRange = 1.0f;
            float attackRadius = 0.5f;

            Vector2 attackOrigin = (Vector2)transform.position + (Vector2)(transform.right * transform.localScale.x * (attackRange * 0.5f));

            int playerLayer = LayerMask.NameToLayer("Player");
            LayerMask playerLayerMask = 1 << playerLayer;

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin, attackRadius, playerLayerMask);

            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                KarakterHareket targetCharacter = hit.GetComponent<KarakterHareket>();
                if (targetCharacter != null && targetCharacter.IsSpawned)
                {
                    int weaponDamage = weaponsScript.weaponData.damage;
                    int totalDamage = CalculateTotalDamage(weaponDamage);

                    targetCharacter.TakeDamage(totalDamage);
                }
            }
        }
    }

    private void HandleRangedWeaponAttack(string weaponName, ulong clientId)
    {
        GameObject bulletPrefab = null;
        
        if (weaponName == "Pistol" && weaponsScript.pistolPrefab != null)
        {
            bulletPrefab = weaponsScript.pistolBulletPrefab;
        }
        else if (weaponName == "Rifle" && weaponsScript.riflePrefab != null)
        {
            bulletPrefab = weaponsScript.rifleBulletPrefab;
        }
        else if (weaponName == "Bow")
        {
            bulletPrefab = weaponsScript.arrowPrefab;
        }
        
        if (bulletPrefab == null)
        {
            return;
        }
        
        NetworkObject bulletNetworkObj = bulletPrefab.GetComponent<NetworkObject>();
        if (bulletNetworkObj == null)
        {
            return;
        }
        
        Vector3 spawnPosition = weaponObject.transform.position;
        
        Vector3 direction = new Vector3(transform.localScale.x, 0, 0).normalized;
        
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        
        NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
        if (bulletNetObj != null)
        {
            try {
                bulletNetObj.Spawn();
            } 
            catch (System.Exception e) {
                Destroy(bullet);
                return;
            }
        }
        
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.Initialize(direction, CalculateTotalDamage(weaponsScript.weaponData.damage), gameObject);
        }
        else
        {
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                float bulletSpeed = 10f;
                bulletRb.velocity = direction * bulletSpeed;
                
                StartCoroutine(DestroyBulletAfterTime(bullet, 5f));
            }
           
        }
    }

    IEnumerator AttackCooldown()
    {
        float currentAttackSpeed = (weaponsScript != null && weaponsScript.weaponData != null && weaponsScript.weaponData.attackSpeed > 0) 
                                   ? weaponsScript.weaponData.attackSpeed 
                                   : this.attackSpeed; 
        
        float cooldownDuration = currentAttackSpeed;
        
        yield return new WaitForSeconds(cooldownDuration);
        canAttack = true;
    }

    [ClientRpc]
    void TriggerAttackAnimationClientRpc(string triggerName)
    {
        if (weaponAnimator == null && weaponObject != null)
        {
            weaponAnimator = weaponObject.GetComponent<Animator>();
        }
        
        if (weaponAnimator != null) 
        {
            weaponAnimator.SetTrigger(triggerName);
        }
        else
        {
            
            if (weaponsScript != null)
            {
                Animator weaponScriptAnimator = weaponObject.GetComponent<Animator>();
                if (weaponScriptAnimator != null)
                {
                    weaponScriptAnimator.SetTrigger(triggerName);
                }
            }
        }
    }

    void HandleAttack()
    {
        bool isOfflineMode = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode();
        
        if (isOfflineMode || IsOwner)
        {
            if (Input.GetMouseButtonDown(0) && canAttack)
            {
                if (isOfflineMode)
                {
                    PerformLocalAttack();
                }
                else
                {
                    AttackServerRpc();
                }
            }
        }
    }

    private void PerformLocalAttack()
    {
        if (weaponObject == null || weaponsScript == null || weaponsScript.weaponData == null)
            return;
            
        string animTrigger = weaponsScript.weaponData.normalAttackTrigger;
        if (string.IsNullOrEmpty(animTrigger))
        {
            switch (weaponsScript.weaponData.weaponName)
            {
                case "Sword":
                    animTrigger = "SwordAttack";
                    break;
                case "Sycthe":
                    animTrigger = "ScytheAttack";
                    break;
                case "Hammer":
                    animTrigger = "HammerAttack";
                    break;
                case "Bow":
                    animTrigger = "BowAttack";
                    break;
                default:
                    animTrigger = "Attack";
                    break;
            }
        }
        
        if (weaponObject != null)
        {
            weaponAnimator = weaponObject.GetComponent<Animator>();
            if (weaponAnimator != null)
            {
                weaponAnimator.SetTrigger(animTrigger);
            }
        }
        
        canAttack = false;
        StartCoroutine(AttackCooldown());
        
        string weaponName = weaponsScript.weaponData.weaponName;
        bool isRangedWeapon = weaponName == "Bow" || weaponName == "Pistol" || weaponName == "Rifle";
        
        if (isRangedWeapon)
        {
            HandleLocalRangedAttack(weaponName);
        }
        else
        {
            float attackRange = 1.0f;
            float attackRadius = 0.5f;
            
            Vector2 attackOrigin = (Vector2)transform.position + (Vector2)(transform.right * transform.localScale.x * (attackRange * 0.5f));
            
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            LayerMask targetMask = 1 << enemyLayer;
            
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin, attackRadius, targetMask);
            
            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                
                Enemies enemy = hit.GetComponent<Enemies>();
                if (enemy != null)
                {
                    int weaponDamage = weaponsScript.weaponData.damage;
                    int totalDamage = weaponDamage + attackDamage;
                    enemy.TakeDamage(totalDamage);
                }
            }
        }
    }
    
    private void HandleLocalRangedAttack(string weaponName)
    {
        GameObject bulletPrefab = null;
        
        if (weaponName == "Pistol" && weaponsScript.pistolPrefab != null)
        {
            bulletPrefab = weaponsScript.pistolBulletPrefab;
        }
        else if (weaponName == "Rifle" && weaponsScript.riflePrefab != null)
        {
            bulletPrefab = weaponsScript.rifleBulletPrefab;
        }
        else if (weaponName == "Bow")
        {
            bulletPrefab = weaponsScript.arrowPrefab;
        }
        
        if (bulletPrefab == null) return;
        
        Vector3 spawnPosition = weaponObject.transform.position;
        Vector3 direction = new Vector3(transform.localScale.x, 0, 0).normalized;
        
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            int totalDamage = weaponsScript.weaponData.damage + attackDamage;
            bulletComponent.Initialize(direction, totalDamage, gameObject);
        }
        else
        {
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                float bulletSpeed = 10f;
                bulletRb.velocity = direction * bulletSpeed;
                
                Destroy(bullet, 5f);
            }
        }
    }

    public int CalculateTotalDamage(int baseWeaponDamage)
    {
        bool isOfflineMode = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode();
        if (isOfflineMode)
        {
            return baseWeaponDamage + attackDamage;
        }
        else
        {
            return baseWeaponDamage + networkAttackDamage.Value; 
        }
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        currentHealth = newValue;
        
        if (greenHealthBar != null && redHealthBar != null)
        {
            UpdateHealthBar();
        }
        
    }

    private void OnLivesChanged(int previousValue, int newValue)
    {
        Debug.Log($"OnLivesChanged - IsOwner: {IsOwner}, ClientID: {OwnerClientId}, NewValue: {newValue}");
        
        if (IsOwner)
        {
        UpdateLifeText(newValue);
        }
        
        if (IsServer && newValue <= 0)
        {
            StartCoroutine(LoadMainMenuAfterDelay());
        }
    }

    private IEnumerator LoadMainMenuAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateLifeText(int lives)
    {
        if (lifeText != null && (IsOwner || IsOfflineMode()))
        {
            Debug.Log($"Updating life text: {lives} (Offline: {IsOfflineMode()})");
            lifeText.text = "Life: " + lives;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReceiveDamageServerRpc(int damage, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        if (isTakingDamage || networkHealth.Value <= 0) return;

        int effectiveDamage = Mathf.Max(0, damage - networkArmor.Value);
        networkHealth.Value -= effectiveDamage;
        networkHealth.Value = Mathf.Clamp(networkHealth.Value, 0, CharHealth);

        TriggerHitFeedbackClientRpc();

        if (networkHealth.Value <= 0)
        {
            HandleDeath();
        }
    }

    [ClientRpc]
    void TriggerHitFeedbackClientRpc()
    {
        if (characterAnimator != null) 
        {
            characterAnimator.SetTrigger("Hit"); 
        }
    }

    private void HandleDeath()
    {
        if (!IsServer) return;

        Debug.Log($"HandleDeath - Current Lives: {networkLives.Value}, ClientID: {OwnerClientId}");
        
        if (networkLives.Value > 0)
        {
            networkLives.Value--;
            Debug.Log($"New Lives after death: {networkLives.Value}");
            
            if (networkLives.Value > 0)
            {
                RespawnPlayer();
            }
            else
            {
                Debug.Log("Permanent death triggered");
                TriggerPermanentDeathFeedbackClientRpc();
                NetworkObject networkObject = GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Despawn(true);
                }
                SceneManager.LoadScene("MainMenu");
            }
        }
    }

    private void RespawnPlayer()
    {
        if (!IsServer) return;

        networkHealth.Value = CharHealth;

        Vector3 spawnPosition = Vector3.zero;
        NetworkPlayerSpawner spawner = FindObjectOfType<NetworkPlayerSpawner>();
        if(spawner != null)
        {   
            spawnPosition = spawner.GetNextSpawnPosition(); 
        } 
        

        if (rb != null) rb.simulated = false;
        transform.position = spawnPosition;
        if (rb != null) 
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }

        RespawnFeedbackClientRpc();
    }

    [ClientRpc]
    private void RespawnFeedbackClientRpc()
    {
        if (IsOfflineMode())
        {
            currentHealth = CharHealth;
        }
        UpdateHealthBar();
    }

    [ClientRpc]
    void TriggerPermanentDeathFeedbackClientRpc() 
    {
         if (characterAnimator != null) 
         {
             characterAnimator.SetTrigger("Die"); 
         }
         
    }

    public void StatGuncelle(string statAdi, int deger)
    {
        bool isOnlineMode = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsOfflineMode();
        
        switch (statAdi)
        {
            case "AD":
                attackDamage += deger;
                if (isOnlineMode && IsServer)
                {
                    networkAttackDamage.Value += deger;
                }
                Debug.Log("Yeni AD: " + attackDamage);
                break;

            case "AP":
                abilityPower += deger;
                if (isOnlineMode && IsServer)
                {
                    networkAbilityPower.Value += deger;
                }
                Debug.Log("Yeni AP: " + abilityPower);
                break;

            case "HEALTH":
                CharHealth += deger;
                currentHealth = Mathf.Clamp(currentHealth + deger, 0, CharHealth);
                
                if (isOnlineMode && IsServer)
                {
                    networkHealth.Value = Mathf.Clamp(networkHealth.Value + deger, 0, CharHealth);
                }
                
                UpdateHealthBar();
                Debug.Log("Yeni Sağlık: " + currentHealth + " / " + CharHealth);
                break;

            case "ARMOR":
                armor += deger;
                if (isOnlineMode && IsServer)
                {
                    networkArmor.Value += deger;
                }
                Debug.Log("Yeni Zırh: " + armor);
                break;

            default:
                Debug.LogWarning("Bilinmeyen stat: " + statAdi);
                break;
        }
    }

    public void Die()
    {
        if (IsOfflineMode())
        {
            offlineLives--;
            Debug.Log($"Offline lives decreased to: {offlineLives}");
            
            UpdateLifeText(offlineLives);
            
            if (offlineLives <= 0)
            {
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                ResetToDefaultStats();
                FindObjectOfType<SaveManager>().SaveGame();
                StartCoroutine(RespawnAfterDelay());
            }
        }
        else
        {
            ReceiveDamageServerRpc(9999);
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        Vector3 spawnPosition = transform.position + Vector3.up * 5f;
        transform.position = spawnPosition;

        currentHealth = CharHealth;
        UpdateLifeText();
        UpdateHealthBar();
    }

    private void ResetToDefaultStats()
    {
        if (IsOfflineMode())
        {
            offlineLives = 3;
            UpdateLifeText(offlineLives);
        }
        else if (IsServer)
        {
            networkLives.Value = 3;
        }
        
        if (defaultStats != null)
        {
            gold = defaultStats.gold;
            CharHealth = defaultStats.maxHealth;
            currentHealth = defaultStats.maxHealth;
            attackDamage = defaultStats.attackDamage;
            abilityPower = defaultStats.abilityPower;
            armor = defaultStats.armor;
            currentWeapon = defaultStats.defaultWeapon;
        }
    }

    public void UpdateHealthBar()
    {
       
        bool isOfflineMode = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode();
        
        if (isOfflineMode)
        {
            float healthRatio = (float)currentHealth / CharHealth;
            greenHealthBar.rectTransform.localScale = new Vector3(healthRatio, 1, 1);
            redHealthBar.enabled = currentHealth < CharHealth;
        }
        else
        {
            float healthRatio = (float)networkHealth.Value / CharHealth;
            greenHealthBar.rectTransform.localScale = new Vector3(healthRatio, 1, 1);
            redHealthBar.enabled = networkHealth.Value < CharHealth;
        }
    }

    public void EquipWeapon(WeaponData weaponData)
    {
        if (weaponsScript == null)
        {
            if (weaponObject != null)
            {
                weaponsScript = weaponObject.GetComponent<WeaponsScript>();
                if (weaponsScript == null)
                {
                    return;
                }
                
            }
            else
            {
                return;
            }
        }
        
        weaponsScript.SetWeaponData(weaponData);
        currentWeapon = weaponData.weaponName;
        
        SpriteRenderer weaponRenderer = weaponObject.GetComponent<SpriteRenderer>();
        if (weaponRenderer != null && weaponData.weaponIcon != null)
        {
            weaponRenderer.sprite = weaponData.weaponIcon;
        }
    }

    [ServerRpc]
    public void EquipWeaponServerRpc(string weaponName, int damage, float attackSpeed, int weaponTypeInt, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        WeaponData weaponData = ScriptableObject.CreateInstance<WeaponData>();
        weaponData.weaponName = weaponName;
        weaponData.damage = damage;
        weaponData.attackSpeed = attackSpeed;
        weaponData.weaponType = (WeaponType)weaponTypeInt;
        

        WeaponData[] allWeapons = Resources.FindObjectsOfTypeAll<WeaponData>();
        foreach (WeaponData existingWeapon in allWeapons)
        {
            if (existingWeapon.weaponName == weaponName)
            {
                weaponData.weaponIcon = existingWeapon.weaponIcon;
                weaponData.normalAttackTrigger = existingWeapon.normalAttackTrigger;
                break;
            }
        }
        
        EquipWeaponClientRpc(weaponName, damage, attackSpeed, weaponTypeInt, clientId);
    }
    
    [ClientRpc]
    private void EquipWeaponClientRpc(string weaponName, int damage, float attackSpeed, int weaponTypeInt, ulong ownerClientId)
    {
        if (OwnerClientId == ownerClientId)
        {
            WeaponData weaponData = ScriptableObject.CreateInstance<WeaponData>();
            weaponData.weaponName = weaponName;
            weaponData.damage = damage;
            weaponData.attackSpeed = attackSpeed;
            weaponData.weaponType = (WeaponType)weaponTypeInt;
            
            WeaponData[] allWeapons = Resources.FindObjectsOfTypeAll<WeaponData>();
            foreach (WeaponData existingWeapon in allWeapons)
            {
                if (existingWeapon.weaponName == weaponName)
                {
                    weaponData.weaponIcon = existingWeapon.weaponIcon;
                    weaponData.normalAttackTrigger = existingWeapon.normalAttackTrigger;
                    break;
                }
            }
            
            EquipWeapon(weaponData);
            
            if (!NetworkManager.Singleton.IsListening)
            {
                SaveManager saveManager = FindObjectOfType<SaveManager>();
                if (saveManager != null)
                {
                    saveManager.SaveGame();
                }
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode()) 
            {
                jumpCounter = 0;
            }
        }
    }

    IEnumerator EnemyDamageOverTime(Enemies enemy)
    {
        isTakingDamage = true;
        
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode())
        {
            while (enemy != null && currentHealth > 0 && isTakingDamage)
            {
                int enemyDamage = enemy.enemyStats != null ? enemy.enemyStats.enemyDamage : 0;
                if (enemyDamage > 0)
                {
                    int effectiveDamage = Mathf.Max(0, enemyDamage - armor);
                    currentHealth -= effectiveDamage;
                    currentHealth = Mathf.Clamp(currentHealth, 0, CharHealth);
                    
                    if (characterAnimator != null)
                    {
                        characterAnimator.SetTrigger("Hit");
                    }
                    
                    UpdateHealthBar();
                    
                    if (currentHealth <= 0)
                    {
                     
                        Die();

                        RespawnAfterDelay();

                        break;
                    }
                }
                
                float enemyAttackSpeed = (enemy.enemyStats != null && enemy.enemyStats.attackSpeed > 0) ? enemy.enemyStats.attackSpeed : 1.0f;
                yield return new WaitForSeconds(1f / enemyAttackSpeed);
            }
        }
        else 
        {
            while (enemy != null && networkHealth.Value > 0 && isTakingDamage)
            {
                if (IsServer) 
                {
                    int enemyDamage = enemy.enemyStats != null ? enemy.enemyStats.enemyDamage : 0;
                    if (enemyDamage > 0)
                    {
                        ReceiveDamageServerRpc(enemyDamage);
                    }
                }
                else
                {
                    isTakingDamage = false;
                    break; 
                }
                
                float enemyAttackSpeed = (enemy.enemyStats != null && enemy.enemyStats.attackSpeed > 0) ? enemy.enemyStats.attackSpeed : 1.0f;
                yield return new WaitForSeconds(1f / enemyAttackSpeed);
            }
        }
        
        isTakingDamage = false;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemies enemy = collision.gameObject.GetComponent<Enemies>();
            if (enemy != null && !isTakingDamage)
            {
                if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode())
                {
                    int enemyDamage = enemy.enemyStats != null ? enemy.enemyStats.enemyDamage : 0;
                    if (enemyDamage > 0 && !isTakingDamage)
                    {
                        isTakingDamage = true;
                        TakeDamage(enemyDamage);
                        
                        StartCoroutine(SinglePlayerDamageCooldown(enemy.enemyStats != null ? 
                            1f / enemy.enemyStats.attackSpeed : 1f));
                    }
                }
                else
                {
                    StartCoroutine(EnemyDamageOverTime(enemy));
                }
            }
        }
    }
    
    private IEnumerator SinglePlayerDamageCooldown(float interval)
    {
        yield return new WaitForSeconds(interval);
        isTakingDamage = false;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            isTakingDamage = false;
        }
    }

    IEnumerator DestroyBulletAfterTime(GameObject bullet, float time)
    {
        yield return new WaitForSeconds(time);
        
        if (bullet != null)
        {
            NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
            if (bulletNetObj != null && bulletNetObj.IsSpawned)
            {
                bulletNetObj.Despawn();
            }
            else
            {
                Destroy(bullet);
            }
        }
    }

    private IEnumerator ChargeAttackServer(SkillData skill)
    {
        if (weaponsScript == null || weaponsScript.weaponData == null)
        {
            yield break;
        }

        string animTrigger = weaponsScript.weaponData.normalAttackTrigger;
        if (string.IsNullOrEmpty(animTrigger))
        {
            switch (weaponsScript.weaponData.weaponName)
            {
                case "Sword":
                    animTrigger = "SwordAttack";
                    break;
                case "Sycthe":
                    animTrigger = "ScytheAttack";
                    break;
                case "Hammer":
                    animTrigger = "HammerAttack";
                    break;
                case "Bow":
                    animTrigger = "BowAttack";
                    break;
                default:
                    animTrigger = "Attack";
                    break;
            }
        }

        TriggerAttackAnimationClientRpc(animTrigger);

        StopCoroutine(AttackCooldown());
        canAttack = true;

        string weaponName = weaponsScript.weaponData.weaponName;
        bool isRangedWeapon = weaponName == "Bow" || weaponName == "Pistol" || weaponName == "Rifle";

        if (isRangedWeapon)
        {
            GameObject bulletPrefab = null;
            
            if (weaponName == "Pistol" && weaponsScript.pistolPrefab != null)
            {
                bulletPrefab = weaponsScript.pistolBulletPrefab;
            }
            else if (weaponName == "Rifle" && weaponsScript.riflePrefab != null)
            {
                bulletPrefab = weaponsScript.rifleBulletPrefab;
            }
            else if (weaponName == "Bow")
            {
                bulletPrefab = weaponsScript.arrowPrefab;
            }

            if (bulletPrefab != null)
            {
                Vector3 spawnPosition = weaponObject.transform.position;
                Vector3 direction = new Vector3(transform.localScale.x, 0, 0).normalized;
                GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
                
                NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
                if (bulletNetObj != null)
                {
                    try 
                    {
                        bulletNetObj.Spawn();
                        
                        Bullet bulletComponent = bullet.GetComponent<Bullet>();
                        if (bulletComponent != null)
                        {
                            int skillDamage = CalculateTotalDamage(weaponsScript.weaponData.damage) * 2;
                            bulletComponent.Initialize(direction, skillDamage, gameObject);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Destroy(bullet);
                    }
                }
            }
        }
        else
        {
            float attackRange = 1.0f;
            float attackRadius = 0.5f;
            Vector2 attackOrigin = (Vector2)transform.position + (Vector2)(transform.right * transform.localScale.x * (attackRange * 0.5f));

            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            LayerMask targetLayers = (1 << playerLayer) | (1 << enemyLayer);

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin, attackRadius, targetLayers);

            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                
                KarakterHareket playerTarget = hit.GetComponent<KarakterHareket>();
                if (playerTarget != null && playerTarget.IsSpawned && playerTarget != this)
                {
                    int weaponDamage = weaponsScript.weaponData.damage;
                    int totalDamage = CalculateTotalDamage(weaponDamage) * 2; 
                    playerTarget.ReceiveDamageServerRpc(totalDamage);
                    continue;
                }

                Enemies enemyTarget = hit.GetComponent<Enemies>();
                if (enemyTarget != null && enemyTarget.IsSpawned)
                {
                    int weaponDamage = weaponsScript.weaponData.damage;
                    int totalDamage = CalculateTotalDamage(weaponDamage) * 2; 
                    enemyTarget.TakeDamageServerRpc(totalDamage);
                }
            }
        }

        yield return new WaitForSeconds(skill.cooldown);
    }

    private IEnumerator AoEAttackServer(SkillData skill)
    {
        GameObject aoeInstance = Instantiate(AoePrefab, transform.position, Quaternion.identity);
        NetworkObject netObj = aoeInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
            
            int skillDamage = (weaponsScript != null && weaponsScript.weaponData != null) 
                ? (int)(weaponsScript.weaponData.damage * skill.damageMultiplier) 
                : (int)(attackDamage * skill.damageMultiplier);
            
            float aoeRadius = 3.0f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
            
            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                
                Enemies enemyTarget = hit.GetComponent<Enemies>();
                if (enemyTarget != null && enemyTarget.IsSpawned)
                {
                    enemyTarget.TakeDamageServerRpc(skillDamage);
                    continue;
                }
                
                KarakterHareket playerTarget = hit.GetComponent<KarakterHareket>();
                if (playerTarget != null && playerTarget.IsSpawned && playerTarget != this)
                {
                    playerTarget.TakeDamage(skillDamage);
                }
            }
            
            yield return new WaitForSeconds(skill.chargeTime);
            
            if (netObj != null && netObj.IsSpawned)
            {
                 netObj.Despawn();
            }
            else
            {
                 Destroy(aoeInstance);
            }
        }
        else
        {
             Destroy(aoeInstance);
        }
    }

    private IEnumerator DashServer(SkillData skill)
    {
        if (rb == null) yield break;
        
        float dashDirection = transform.localScale.x > 0 ? 1f : -1f;
        float dashForce = 60f;
        float dashDuration = 0.4f;
        int dashDamage = 15;
        float dashDistance = 5f;

        int originalLayer = gameObject.layer;
        gameObject.layer = LayerMask.NameToLayer("Invulnerable");

        Vector2 originalVelocity = rb.velocity;
        rb.velocity = Vector2.zero;

        Vector2 targetPosition = (Vector2)transform.position + Vector2.right * dashDirection * dashDistance;

        float elapsedTime = 0f;
        Vector2 startPosition = transform.position;
        
        while (elapsedTime < dashDuration)
        {
            float t = elapsedTime / dashDuration;
            t = Mathf.SmoothStep(0, 1, t);
            transform.position = Vector2.Lerp(startPosition, targetPosition, t);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.8f);
            foreach(var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                
                Enemies enemy = hit.GetComponent<Enemies>();
                if (enemy != null && enemy.IsSpawned)
                {
                    enemy.TakeDamageServerRpc(dashDamage);
                    continue;
                }
                
                KarakterHareket playerTarget = hit.GetComponent<KarakterHareket>();
                if (playerTarget != null && playerTarget.IsSpawned && playerTarget != this)
                {
                    playerTarget.ReceiveDamageServerRpc(dashDamage);
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        gameObject.layer = originalLayer;
        
        rb.velocity = originalVelocity;
    }

    private void HealServer(SkillData skill)
    {
        int healAmount = Mathf.Max(20, (int)skill.damageMultiplier);
        
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode())
        {
            currentHealth = Mathf.Clamp(currentHealth + 300, 0, CharHealth);
            UpdateHealthBar();
        }
        else
        {
            networkHealth.Value = Mathf.Clamp(networkHealth.Value + 300, 0, CharHealth);
            
            TriggerHealEffectClientRpc(OwnerClientId);
            
            float healRadius = 5.0f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, healRadius);
            
            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                
                KarakterHareket playerTarget = hit.GetComponent<KarakterHareket>();
                if (playerTarget != null && playerTarget.IsSpawned)
                {
                    
                    playerTarget.networkHealth.Value = Mathf.Clamp(playerTarget.networkHealth.Value + healAmount, 0, playerTarget.CharHealth);
                    TriggerHealEffectClientRpc(playerTarget.OwnerClientId);
                }
            }
        }
    }
    
    [ClientRpc]
    private void TriggerHealEffectClientRpc(ulong targetClientId)
    {
        
        if (OwnerClientId == targetClientId)
        {
            if (greenHealthBar != null && redHealthBar != null)
            {
                UpdateHealthBar();
            }
        }
    }

    [ClientRpc]
    void TriggerSkillCooldownClientRpc(int skillIndex, ClientRpcParams clientRpcParams = default)
    {
        SkillTreeManager manager = FindObjectOfType<SkillTreeManager>();
        
        if (manager != null && skillIndex >= 0 && skillIndex < manager.skillIcons.Count)
        {
            if (skillIndex < manager.skills.Count)
            {
                 SkillData skill = manager.skills[skillIndex];
                 manager.skillIcons[skillIndex]?.StartCooldown(skill.cooldown);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0)
            return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode())
        {
            int effectiveDamage = Mathf.Max(0, damage - armor);
            currentHealth -= effectiveDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, CharHealth);
            
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger("Hit");
            }
            
            UpdateHealthBar();
            
            if (currentHealth <= 0)
            {
                Debug.Log(lifeText);
                Die();
            }
        }
        else
        {
            ReceiveDamageServerRpc(damage);
        }
    }

    private void UpdateLifeText()
    {
        if (lifeText != null)
        {
            lifeText.text = "Life: " + offlineLives;
        }
    }
}


