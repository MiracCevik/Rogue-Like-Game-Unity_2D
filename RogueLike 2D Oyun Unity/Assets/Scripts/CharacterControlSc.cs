using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using Unity.Netcode;

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
    private NetworkVariable<int> networkLives = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>(); 
        if (rb == null)
        {
            enabled = false; 
            return;
        }
        

        if (IsOwner)
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
            enabled = false; 

            Camera playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera != null) 
            {
                 playerCamera.gameObject.SetActive(false); 
                 AudioListener listener = playerCamera.GetComponent<AudioListener>();
                 if (listener != null) listener.enabled = false;
            }
            
            if (rb != null) 
            {
                if (IsServer) 
                { 
                    rb.isKinematic = false; 
                } 
                else 
                { 
                    rb.isKinematic = true; 
                }
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

        currentHealth = networkHealth.Value; 
        UpdateHealthBar();
    }

    void Awake()
    {
        if (!NetworkManager.Singleton.IsListening)
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
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }
    }

    void Update()
    {
        if (!NetworkManager.Singleton.IsListening)
        {
            return;
        }

        if (!IsOwner) return;

        float hareketInputX = Input.GetAxis("Horizontal");
        bool jumpPressed = Input.GetButtonDown("Jump");

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

        SubmitMovementInputServerRpc(hareketInputX, jumpPressed);

        HandleAttack(); 
        HandleSkillInput(); 
    }

    void HandleSkillInput()
    {
        if (Input.GetKeyDown(KeyCode.Q)) UseSkill(0);
        if (Input.GetKeyDown(KeyCode.E)) UseSkill(1);
        if (Input.GetKeyDown(KeyCode.Z)) UseSkill(2);
        if (Input.GetKeyDown(KeyCode.C)) UseSkill(3);
    }

    public void UseSkill(int skillIndex)
    {
        SkillTreeManager manager = FindObjectOfType<SkillTreeManager>();
        manager.UseSkill(skillIndex, gameObject);
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
        else
        {
            return;
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
        var clientId = rpcParams.Receive.SenderClientId;

        if (!canAttack || weaponsScript == null || weaponsScript.weaponData == null)
        {
             return;
        }

        if (weaponAnimator == null && weaponObject != null)
        {
            weaponAnimator = weaponObject.GetComponent<Animator>();
        }

        string animTrigger = weaponsScript.weaponData.normalAttackTrigger;
        if (string.IsNullOrEmpty(animTrigger))
        {
            if (weaponsScript.weaponData.weaponName == "Scythe")
            {
                animTrigger = "ScytheAttack";
            }
            else
            {
                animTrigger = "Attack";
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


                    targetCharacter.ReceiveDamageServerRpc(totalDamage);
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
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0) && canAttack)
        {
             AttackServerRpc();
        }
    }

    public int CalculateTotalDamage(int baseWeaponDamage)
    {
        return baseWeaponDamage + networkAttackDamage.Value;
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        currentHealth = newValue;
        UpdateHealthBar();

       
    }

    private void OnLivesChanged(int previousValue, int newValue)
    {
        
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

        if (networkLives.Value > 0)
        {
            networkLives.Value--;
            RespawnPlayer();
        }
        else
        {
            TriggerPermanentDeathFeedbackClientRpc();
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Despawn(true);
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
        
        Collider2D mainCollider = GetComponent<Collider2D>();
        if (mainCollider != null) mainCollider.enabled = true;
        
        if (rb != null) rb.simulated = true; 
        
        this.enabled = true; 

        UpdateHealthBar();

        if (characterAnimator != null)
        {
        }
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
        switch (statAdi)
        {
            case "AD":
                attackDamage += deger;
                Debug.Log("Yeni AD: " + attackDamage);
                break;

            case "AP":
                abilityPower += deger;
                Debug.Log("Yeni AP: " + abilityPower);
                break;

            case "HEALTH":
                CharHealth += deger;
                currentHealth = Mathf.Clamp(currentHealth + deger, 0, CharHealth);
                UpdateHealthBar();
                Debug.Log("Yeni Sağlık: " + currentHealth + " / " + CharHealth);
                break;

            case "ARMOR":
                armor += deger;
                Debug.Log("Yeni Zırh: " + armor);
                break;

            default:
                Debug.LogWarning("Bilinmeyen stat: " + statAdi);
                break;
        }
    }

    public void Die()
    {
        Debug.Log("Karakter Öldü!");
        ResetToDefaultStats();
        FindObjectOfType<SaveManager>().SaveGame();
        Debug.Log("Die dan sonra save alındı");
        SceneManager.LoadScene("Level1");
    }
    private void ResetToDefaultStats()
    {
        if (defaultStats != null)
        {
            networkLives.Value = 3;
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
        float healthRatio = (float)currentHealth / CharHealth;
        greenHealthBar.rectTransform.localScale = new Vector3(healthRatio, 1, 1);
        redHealthBar.enabled = currentHealth < CharHealth;
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
        
        SpriteRenderer weaponRenderer = weaponObject.GetComponent<SpriteRenderer>();
        if (weaponRenderer != null && weaponData.weaponIcon != null)
        {
            weaponRenderer.sprite = weaponData.weaponIcon;
        }
  
    
      

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (IsServer) 
            {
                jumpCounter = 0;
            }
        }
    }

    IEnumerator EnemyDamageOverTime(Enemies enemy)
    {
        isTakingDamage = true;
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

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemies enemy = collision.gameObject.GetComponent<Enemies>();
            if (enemy != null && !isTakingDamage)
            {
                StartCoroutine(EnemyDamageOverTime(enemy));
            }
        }
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
}
