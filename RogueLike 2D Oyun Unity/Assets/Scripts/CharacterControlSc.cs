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

        // Silah Animator kontrolü
        if (weaponAnimator == null && weaponObject != null)
        {
            weaponAnimator = weaponObject.GetComponent<Animator>();
            Debug.LogWarning($"[Server - Sender: {clientId}] Silah Animator otomatik olarak yeniden tanımlandı.");
        }

        string animTrigger = weaponsScript.weaponData.normalAttackTrigger;
        if (string.IsNullOrEmpty(animTrigger))
        {
            // Silah tipine göre varsayılan trigger isimlerini ayarla
            if (weaponsScript.weaponData.weaponName == "Scythe")
            {
                animTrigger = "ScytheAttack";
                Debug.Log("Scythe için özel animTrigger ayarlandı: " + animTrigger);
            }
            else
            {
                animTrigger = "Attack";
                Debug.LogWarning($"[Server - Sender: {clientId}] WeaponData ({weaponsScript.weaponData.name}) içinde normalAttackTrigger tanımlı değil. Varsayılan '{animTrigger}' kullanılıyor.");
            }
        }

        TriggerAttackAnimationClientRpc(animTrigger);

        canAttack = false;
        StartCoroutine(AttackCooldown());

        // Uzak menzilli silah kontrolü
        string weaponName = weaponsScript.weaponData.weaponName;
        bool isRangedWeapon = weaponName == "Bow" || weaponName == "Pistol" || weaponName == "Rifle";
        
        if (isRangedWeapon)
        {
            // Uzak menzilli silah için mermi oluştur
            HandleRangedWeaponAttack(weaponName, clientId);
        }
        else
        {
            // Yakın dövüş silahı için mevcut mantık
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
    }

    private void HandleRangedWeaponAttack(string weaponName, ulong clientId)
    {
        // Silah tipine göre uygun mermi prefabını belirle
        GameObject bulletPrefab = null;
        
        if (weaponName == "Pistol" && weaponsScript.pistolPrefab != null)
        {
            bulletPrefab = weaponsScript.pistolBulletPrefab;
            Debug.Log($"[Server - Sender: {clientId}] Pistol için bulletPrefab: {(bulletPrefab != null ? bulletPrefab.name : "null")}");
        }
        else if (weaponName == "Rifle" && weaponsScript.riflePrefab != null)
        {
            bulletPrefab = weaponsScript.rifleBulletPrefab;
            Debug.Log($"[Server - Sender: {clientId}] Rifle için bulletPrefab: {(bulletPrefab != null ? bulletPrefab.name : "null")}");
        }
        else if (weaponName == "Bow")
        {
            bulletPrefab = weaponsScript.arrowPrefab;
            Debug.Log($"[Server - Sender: {clientId}] Bow için bulletPrefab: {(bulletPrefab != null ? bulletPrefab.name : "null")}");
        }
        
        if (bulletPrefab == null)
        {
            Debug.LogError($"[Server - Sender: {clientId}] {weaponName} için mermi prefabı bulunamadı!");
            return;
        }
        
        // NetworkObject bileşeni kontrolü
        NetworkObject bulletNetworkObj = bulletPrefab.GetComponent<NetworkObject>();
        if (bulletNetworkObj == null)
        {
            Debug.LogError($"[Server - Sender: {clientId}] {weaponName} mermi prefabında NetworkObject bileşeni yok! Lütfen prefaba NetworkObject ekleyin.");
            return;
        }
        
        // Merminin başlangıç pozisyonu (silahın ucu)
        Vector3 spawnPosition = weaponObject.transform.position;
        
        // Merminin yönü (karakterin baktığı yön)
        Vector3 direction = new Vector3(transform.localScale.x, 0, 0).normalized;
        
        // Mermiyi oluştur
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        
        // Mermi nesnesini ağda görünür yap
        NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
        if (bulletNetObj != null)
        {
            try {
                bulletNetObj.Spawn();
                Debug.Log($"[Server - Sender: {clientId}] {weaponName} için mermi ağda spawn edildi.");
            } 
            catch (System.Exception e) {
                Debug.LogError($"[Server - Sender: {clientId}] Mermi spawn edilirken hata: {e.Message}");
                Destroy(bullet);
                return;
            }
        }
        else
        {
            Debug.LogError($"[Server - Sender: {clientId}] Mermi nesnesinde NetworkObject bileşeni bulunamadı!");
        }
        
        // Mermi hareketini başlat
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.Initialize(direction, CalculateTotalDamage(weaponsScript.weaponData.damage), gameObject);
            Debug.Log($"[Server - Sender: {clientId}] Mermi başlatıldı. Yön: {direction}, Hasar: {weaponsScript.weaponData.damage}");
        }
        else
        {
            // Eğer özel Bullet bileşeni yoksa, basit bir hareket ekleyelim
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                float bulletSpeed = 10f;
                bulletRb.velocity = direction * bulletSpeed;
                Debug.Log($"[Server - Sender: {clientId}] Mermi fizik ile hareket ettiriliyor. Hız: {bulletSpeed}");
                
                // Otomatik yok etme için coroutine başlat
                StartCoroutine(DestroyBulletAfterTime(bullet, 5f));
            }
            else
            {
                Debug.LogError($"[Server - Sender: {clientId}] Mermi nesnesinde Rigidbody2D bileşeni bulunamadı!");
            }
        }
    }

    IEnumerator AttackCooldown()
    {
        // AttackSpeed değerini doğrudan saniye cinsinden bekleme süresi olarak kullan
        float currentAttackSpeed = (weaponsScript != null && weaponsScript.weaponData != null && weaponsScript.weaponData.attackSpeed > 0) 
                                   ? weaponsScript.weaponData.attackSpeed 
                                   : this.attackSpeed; 
        
        // Artık 1.0f'e bölmeye gerek yok, doğrudan süre olarak kullan
        float cooldownDuration = currentAttackSpeed;
        
        yield return new WaitForSeconds(cooldownDuration);
        canAttack = true;
        Debug.Log($"[Server] Attack cooldown bitti ({cooldownDuration}s). Tekrar saldırılabilir.");
    }

    [ClientRpc]
    void TriggerAttackAnimationClientRpc(string triggerName)
    {
        // Silah animatörünün güncel olduğundan emin olalım
        if (weaponAnimator == null && weaponObject != null)
        {
            weaponAnimator = weaponObject.GetComponent<Animator>();
            Debug.LogWarning($"[Client {NetworkManager.Singleton.LocalClientId}] weaponAnimator null olduğu için otomatik olarak alındı.");
        }
        
        if (weaponAnimator != null) 
        {
            weaponAnimator.SetTrigger(triggerName);
            Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Silah animasyonu tetiklendi: {triggerName} (Animator: {weaponAnimator.name})");
        }
        else
        {
            Debug.LogError($"[Client {NetworkManager.Singleton.LocalClientId}] weaponAnimator bulunamadı, silah animasyonu tetiklenemiyor: {triggerName}");
            
            // Düzeltmeye çalış: Silah nesnesindeki WeaponsScript üzerindeki animator'ı kullan
            if (weaponsScript != null)
            {
                Animator weaponScriptAnimator = weaponObject.GetComponent<Animator>();
                if (weaponScriptAnimator != null)
                {
                    weaponScriptAnimator.SetTrigger(triggerName);
                    Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Alternatif animator (WeaponsScript üzerindeki) kullanılarak animasyon tetiklendi.");
                }
            }
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
            if (weaponObject != null)
            {
                weaponsScript = weaponObject.GetComponent<WeaponsScript>();
                if (weaponsScript == null)
                {
                    Debug.LogError("Weapon objesi üzerinde WeaponsScript bulunamadı!");
                    return;
                }
                else
                {
                    Debug.Log("WeaponsScript referansı otomatik olarak alındı");
                }
            }
            else
            {
                Debug.LogError("weaponObject null, WeaponsScript bulunamıyor!");
                return;
            }
        }
        
        // Weapon data atama ve silah nesnelerini yönetme
        weaponsScript.SetWeaponData(weaponData);
        
        // Sprite güncelleme
        SpriteRenderer weaponRenderer = weaponObject.GetComponent<SpriteRenderer>();
        if (weaponRenderer != null && weaponData.weaponIcon != null)
        {
            weaponRenderer.sprite = weaponData.weaponIcon;
            Debug.Log($"{weaponData.weaponName} için sprite güncellendi: {weaponData.weaponIcon.name}");
        }
        else
        {
            Debug.LogWarning("weaponRenderer veya weaponData.weaponIcon null!");
        }

        // Weapon animator referansını güncelleme
        weaponAnimator = weaponObject.GetComponent<Animator>();
        if (weaponAnimator == null)
        {
            Debug.LogError("Weapon Animator bulunamadı!");
        }
        else
        {
            Debug.Log($"Weapon Animator güncellendi: {weaponAnimator.name}");
            
            // AnimatorController kontrolü
            if (weaponAnimator.runtimeAnimatorController != null)
            {
                Debug.Log($"Animator controller: {weaponAnimator.runtimeAnimatorController.name}");
                
                // İlgili prefabları kontrol et
                if (weaponData.weaponName == "Rifle" || weaponData.weaponName == "Pistol")
                {
                    Debug.Log($"{weaponData.weaponName} için prefab kontrolü yapılıyor");
                    
                    // Riffle prefab kontrol
                    if (weaponData.weaponName == "Rifle" && weaponsScript.riflePrefab != null)
                    {
                        Animator rifleAnimator = weaponsScript.riflePrefab.GetComponent<Animator>();
                        if (rifleAnimator != null && rifleAnimator.runtimeAnimatorController != null)
                        {
                            Debug.Log($"Rifle prefab animator controller: {rifleAnimator.runtimeAnimatorController.name}");
                        }
                        else
                        {
                            Debug.LogWarning("Rifle prefab animator controller yok!");
                        }
                    }
                    
                    // Pistol prefab kontrol
                    if (weaponData.weaponName == "Pistol" && weaponsScript.pistolPrefab != null)
                    {
                        Animator pistolAnimator = weaponsScript.pistolPrefab.GetComponent<Animator>();
                        if (pistolAnimator != null && pistolAnimator.runtimeAnimatorController != null)
                        {
                            Debug.Log($"Pistol prefab animator controller: {pistolAnimator.runtimeAnimatorController.name}");
                        }
                        else
                        {
                            Debug.LogWarning("Pistol prefab animator controller yok!");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Weapon Animator'da RuntimeAnimatorController yok!");
            }
            
            // Silah türüne göre ilgili parametreleri ayarla
            weaponAnimator.SetTrigger("Reset");
        }

        Debug.Log($"Yeni Silah Donatıldı: {weaponData.weaponName}");
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
