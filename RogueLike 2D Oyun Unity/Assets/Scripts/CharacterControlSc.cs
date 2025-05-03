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
            Debug.LogError($"[{OwnerClientId}] OnNetworkSpawn: Rigidbody2D bulunamadı!");
            enabled = false; 
            return;
        }
        characterAnimator = GetComponent<Animator>();
        if (characterAnimator == null)
        {
             Debug.LogError($"[{OwnerClientId}] OnNetworkSpawn: Ana karakter Animator bulunamadı!");
        }

        if (IsOwner)
        {
            enabled = true; 
            Debug.Log($"[{OwnerClientId}] OnNetworkSpawn: IsOwner=True. Script ETKİNLEŞTİRİLDİ.");

            Camera playerCamera = GetComponentInChildren<Camera>(true); 
            if (playerCamera != null) 
            { 
                playerCamera.gameObject.SetActive(true);
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
                Debug.Log($"[{OwnerClientId}] Owner camera/listener etkinleştirildi.");
            } 
            else { Debug.LogError($"[{OwnerClientId}] Owner: Alt kamera bulunamadı!"); }
            
            if (weaponObject != null) 
            { 
                weaponsScript = weaponObject.GetComponent<WeaponsScript>(); 
                weaponAnimator = weaponObject.GetComponent<Animator>();
                if (weaponsScript == null) Debug.LogError($"[{OwnerClientId}] Owner: WeaponObject üzerinde WeaponsScript bulunamadı!");
                if (weaponAnimator == null) Debug.LogError($"[{OwnerClientId}] Owner: WeaponObject üzerinde Animator bulunamadı!");
            }
            else { Debug.LogWarning($"[{OwnerClientId}] Owner: weaponObject atanmamış."); }
           
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
                    Debug.Log($"[{OwnerClientId}] OnNetworkSpawn: IsOwner=False, IsServer=True. Rigidbody DYNAMIC bırakıldı.");
                } 
                else 
                { 
                    rb.isKinematic = true; 
                    Debug.Log($"[{OwnerClientId}] OnNetworkSpawn: IsOwner=False, IsClient=True. Rigidbody KINEMATIC yapıldı.");
                }
            }
            Debug.Log($"[{OwnerClientId}] OnNetworkSpawn: IsOwner=False. Script DEVRE DIŞI BIRAKILDI.");
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
            if (IsServer) Debug.LogError($"[Server] SubmitMovementInputServerRpc: Rigidbody is null! GameObject: {gameObject.name}");
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
        else
        {
            Debug.LogError($"[Client {OwnerClientId}] Transform null!");
        }
    }

    [ServerRpc]
    public void AttackServerRpc(ServerRpcParams rpcParams = default)
    {
        var clientId = rpcParams.Receive.SenderClientId;

        if (!canAttack || weaponsScript == null || weaponsScript.weaponData == null)
        {
             Debug.Log($"[Server - Sender: {clientId}] AttackServerRpc: Saldırı yapılamaz. canAttack: {canAttack}, weaponsScript: {weaponsScript?.GetInstanceID()}, weaponData: {weaponsScript?.weaponData?.name}");
             return;
        }

        string animTrigger = weaponsScript.weaponData.normalAttackTrigger;
        if (string.IsNullOrEmpty(animTrigger))
        {
            animTrigger = "Attack";
            Debug.LogWarning($"[Server - Sender: {clientId}] WeaponData ({weaponsScript.weaponData.name}) içinde normalAttackTrigger tanımlı değil. Varsayılan '{animTrigger}' kullanılıyor.");
        }

        TriggerAttackAnimationClientRpc(animTrigger);

        canAttack = false;
        StartCoroutine(AttackCooldown());

        float attackRange = 1.0f;
        float attackRadius = 0.5f;

        Vector2 attackOrigin = (Vector2)transform.position + (Vector2)(transform.right * transform.localScale.x * (attackRange * 0.5f));

        int playerLayer = LayerMask.NameToLayer("Player");
        LayerMask playerLayerMask = 1 << playerLayer;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin, attackRadius, playerLayerMask);

        Debug.Log($"[Server - Sender: {clientId}] Saldırı yapıldı. Origin: {attackOrigin}, Radius: {attackRadius}, LayerMask: {LayerMask.LayerToName(playerLayer)}, Vurulan Collider Sayısı: {hits.Length}");

        foreach (Collider2D hit in hits)
        {
            // Detaylı log ekle
            Debug.Log($"[Server - Sender: {clientId}] Hit: {hit.gameObject.name}, Layer: {LayerMask.LayerToName(hit.gameObject.layer)}, Tag: {hit.tag}, Is Trigger: {hit.isTrigger}");

            if (hit.gameObject == gameObject) continue;

            KarakterHareket targetCharacter = hit.GetComponent<KarakterHareket>();
            if (targetCharacter != null && targetCharacter.IsSpawned)
            {
                int weaponDamage = weaponsScript.weaponData.damage;
                int totalDamage = CalculateTotalDamage(weaponDamage);

                Debug.Log($"[Server - Sender: {clientId}] Oyuncu {targetCharacter.OwnerClientId} ({hit.gameObject.name}) vuruldu. Hasar: {totalDamage}");

                targetCharacter.ReceiveDamageServerRpc(totalDamage);
            }
            else
            {
                 Debug.LogWarning($"[Server - Sender: {clientId}] Vurulan collider ({hit.name}) KarakterHareket bileşenine sahip değil veya spawn edilmemiş.");
            }
        }
    }

    IEnumerator AttackCooldown()
    {
        float currentAttackSpeed = (weaponsScript != null && weaponsScript.weaponData != null && weaponsScript.weaponData.attackSpeed > 0) 
                                   ? weaponsScript.weaponData.attackSpeed 
                                   : this.attackSpeed; 
        float cooldownDuration = 1.0f / (currentAttackSpeed > 0 ? currentAttackSpeed : 1.0f);
        yield return new WaitForSeconds(cooldownDuration);
        canAttack = true;
        Debug.Log($"[Server] Attack cooldown bitti ({cooldownDuration}s). Tekrar saldırılabilir.");
    }

    [ClientRpc]
    void TriggerAttackAnimationClientRpc(string triggerName)
    {
        if (weaponAnimator != null) 
        {
            weaponAnimator.SetTrigger(triggerName);
             Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Silah animasyonu tetiklendi: {triggerName} (Animator: {weaponAnimator.name})");
        }
        else
        {
             Debug.LogError($"[Client {NetworkManager.Singleton.LocalClientId}] weaponAnimator bulunamadı, silah animasyonu tetiklenemiyor: {triggerName}");
        }
    }

    void HandleAttack()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0) && canAttack)
        {
             Debug.Log($"[{OwnerClientId}] Saldırı tuşuna basıldı. AttackServerRpc çağrılıyor.");
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
        Debug.Log($"[{OwnerClientId} / LocalClient: {NetworkManager.Singleton.LocalClientId}] Sağlık değişti: {previousValue} -> {newValue}. UI güncellendi.");

        if (IsOwner && newValue < previousValue)
        {
           // TriggerHitFeedback();
        }
    }

    private void OnLivesChanged(int previousValue, int newValue)
    {
         Debug.Log($"[{OwnerClientId} / LocalClient: {NetworkManager.Singleton.LocalClientId}] Can hakkı değişti: {previousValue} -> {newValue}.");
         // TODO: Update lives UI if needed
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReceiveDamageServerRpc(int damage, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        if (isTakingDamage || networkHealth.Value <= 0) return;

        int effectiveDamage = Mathf.Max(0, damage - networkArmor.Value);
        networkHealth.Value -= effectiveDamage;
        networkHealth.Value = Mathf.Clamp(networkHealth.Value, 0, CharHealth);

        Debug.Log($"[Server - {OwnerClientId}] ReceiveDamageServerRpc: Hasar aldı. Gelen Hasar: {damage}, Zırh: {networkArmor.Value}, Efektif Hasar: {effectiveDamage}, Yeni Sağlık: {networkHealth.Value}");

        TriggerHitFeedbackClientRpc();

        if (networkHealth.Value <= 0)
        {
            Debug.Log($"[Server - {OwnerClientId}] Sağlık <= 0. HandleDeath çağrılıyor.");
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
        else
        {
            Debug.LogError($"[Client {NetworkManager.Singleton.LocalClientId}] characterAnimator is null, cannot trigger Hit animation.");
        }
         Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Hasar geri bildirimi tetiklendi (Hit animasyonu vb.).");
    }

    private void HandleDeath()
    {
        if (!IsServer) return;

        if (networkLives.Value > 0)
        {
            networkLives.Value--;
            Debug.Log($"[Server - {OwnerClientId}] Bir can kaybedildi. Kalan can: {networkLives.Value}. RespawnPlayer çağrılıyor.");
            RespawnPlayer();
        }
        else
        {
            Debug.Log($"[Server - {OwnerClientId}] Can hakkı bitti ({networkLives.Value}). NetworkObject kalıcı olarak despawn ediliyor.");
            TriggerPermanentDeathFeedbackClientRpc();
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Despawn(true);
            }
            else
            {
                 Debug.LogError($"[Server - {OwnerClientId}] HandleDeath (Permanent): NetworkObject bulunamadı! Despawn edilemiyor.");
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
            Debug.Log($"[Server - {OwnerClientId}] NetworkPlayerSpawner bulundu. Spawn noktası: {spawnPosition}");
        }
        else
        {    
            Debug.LogWarning($"[Server - {OwnerClientId}] NetworkPlayerSpawner bulunamadı! Varsayılan (0,0,0) spawn noktası kullanılıyor.");
        }

        if (rb != null) rb.simulated = false;
        transform.position = spawnPosition;
        if (rb != null) 
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }
        Debug.Log($"[Server - {OwnerClientId}] Oyuncu {spawnPosition} noktasına ışınlandı.");

        RespawnFeedbackClientRpc();
    }

    [ClientRpc]
    private void RespawnFeedbackClientRpc()
    {
        Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] RespawnFeedbackClientRpc alındı. Bileşenler etkinleştiriliyor.");
        
        Collider2D mainCollider = GetComponent<Collider2D>();
        if (mainCollider != null) mainCollider.enabled = true;
        
        if (rb != null) rb.simulated = true; 
        
        this.enabled = true; 

        UpdateHealthBar();

        if (characterAnimator != null)
        {
            // characterAnimator.SetTrigger("Respawn");
        }
    }

    [ClientRpc]
    void TriggerPermanentDeathFeedbackClientRpc() 
    {
         Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Kalıcı Ölüm geri bildirimi tetiklendi.");
         if (characterAnimator != null) 
         {
             characterAnimator.SetTrigger("Die"); 
         }
         else
         {
             Debug.LogError($"[Client {NetworkManager.Singleton.LocalClientId}] characterAnimator is null, cannot trigger Die animation.");
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

            Debug.Log($"Statlar sıfırlandı -> Gold: {gold}, Health: {CharHealth}, AttackDamage: {attackDamage}");
        }
        else
        {
            Debug.LogError("DefaultCharacterStats referansı atanmamış!");
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
            Debug.LogError("WeaponsScript referansı bulunamadı!");
            return;
        }
        weaponsScript.weaponData = weaponData;
        SpriteRenderer weaponRenderer = weaponObject.GetComponent<SpriteRenderer>();
        if (weaponRenderer != null && weaponData.weaponIcon != null)
        {
            weaponRenderer.sprite = weaponData.weaponIcon;
        }

        Debug.Log($"Yeni Silah Donatıldı: {weaponData.weaponName}");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (IsServer) 
            {
                Debug.Log("[Server] Yere temas algılandı. Zıplama sayacı sıfırlanıyor.");
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
                    Debug.Log($"[Server - {OwnerClientId}] EnemyDamageOverTime: Düşman hasarı alındı: {enemyDamage}");
                 }
            }
            else
            {
                 Debug.LogWarning($"[Client - {OwnerClientId}] EnemyDamageOverTime running on client, stopping damage application.");
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
}
