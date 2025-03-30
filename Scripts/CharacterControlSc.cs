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

    public Animator animator;
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

    public override void OnNetworkSpawn()
    {
        // Çevrimdýþý mod için kontrol
        if (!NetworkManager.Singleton.IsListening)
        {
            enabled = true;
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();

            if (weaponObject != null)
            {
                weaponsScript = weaponObject.GetComponent<WeaponsScript>();
            }

            UpdateHealthBar();
            return;
        }

        // Çevrimiçi mod için kontrol
        if (IsOwner)
        {
            enabled = true;
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();

            if (weaponObject != null)
            {
                weaponsScript = weaponObject.GetComponent<WeaponsScript>();
            }

            UpdateHealthBar();
        }
    }

    void Awake()
    {
        // Çevrimdýþý modda normal singleton
        if (!NetworkManager.Singleton.IsListening)
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                enabled = true;
                animator = GetComponent<Animator>();
                rb = GetComponent<Rigidbody2D>();
                if (weaponObject != null)
                {
                    weaponsScript = weaponObject.GetComponent<WeaponsScript>();
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
        // Çevrimdýþý modda tüm inputlarý etkinleþtir
        if (!NetworkManager.Singleton.IsListening)
        {
            HandleMovement();
            HandleAttack();
            HandleSkillInput();
            return;
        }

        // Çevrimiçi modda sadece owner için inputlarý etkinleþtir
        if (!IsOwner) return;
        HandleMovement();
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


    void HandleMovement()
    {
        float hareketInputX = Input.GetAxis("Horizontal");
        Vector3 newPosition = transform.position + new Vector3(hareketInputX * hiz * Time.deltaTime, 0, 0);
        transform.position = newPosition;

        if (hareketInputX > 0)
        {
            animator.SetBool("isRunning", true);
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (hareketInputX < 0)
        {
            animator.SetBool("isRunning", true);
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }

        if (Input.GetButtonDown("Jump") && jumpCounter < maxJumpCount)
        {
            animator.SetTrigger("isJumping");
            rb.velocity = new Vector2(rb.velocity.x, 0);
            rb.AddForce(Vector2.up * ziplama, ForceMode2D.Impulse);
            jumpCounter++;
        }
    }

    [ServerRpc]
    public void AttackServerRpc()
    {
        if (weaponsScript != null)
        {
            weaponsScript.EnableHitbox();
            animator.SetTrigger(weaponsScript.weaponData.normalAttackTrigger);
            weaponsScript.PlayAttackAnimation();
            weaponsScript.Attack(CalculateTotalDamage());
        }
    }

    void HandleAttack()
    {
        if (weaponsScript != null)
        {
            attackSpeed = weaponsScript.GetAttackSpeed();
        }
        if (Input.GetButtonDown("Fire1") && canAttack)
        {
            AttackServerRpc();
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        canAttack = false;
        if (weaponsScript != null && !string.IsNullOrEmpty(weaponsScript.weaponData.normalAttackTrigger))
        {
            weaponsScript.EnableHitbox();
            animator.SetTrigger(weaponsScript.weaponData.normalAttackTrigger);
            weaponsScript.PlayAttackAnimation();
            weaponsScript.Attack(CalculateTotalDamage());
        }

        yield return new WaitForSeconds(attackSpeed);

        canAttack = true;

    }


    public int CalculateTotalDamage()
    {
        int weaponDamage = weaponsScript != null ? weaponsScript.weaponData.damage : 0;
        return attackDamage + weaponDamage;
    }


    public void TakeDamage(int damage)
    {
        if (currentHealth > 0)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, CharHealth);
            UpdateHealthBar();

            if (currentHealth <= 0)
            {
                Die();
            }
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
                Debug.Log("Yeni Saðlýk: " + currentHealth + " / " + CharHealth);
                break;

            case "ARMOR":
                armor += deger;
                Debug.Log("Yeni Zýrh: " + armor);
                break;

            default:
                Debug.LogWarning("Bilinmeyen stat: " + statAdi);
                break;
        }
    }

    public void Die()
    {
        Debug.Log("Karakter öldü!");
        ResetToDefaultStats();
        FindObjectOfType<SaveManager>().SaveGame();
        Debug.Log("Die dan sonra save alýndý");
        SceneManager.LoadScene("Level1");
    }
    private void ResetToDefaultStats()
    {
        if (defaultStats != null)
        {
            gold = defaultStats.gold;
            CharHealth = defaultStats.maxHealth;
            currentHealth = defaultStats.maxHealth;
            attackDamage = defaultStats.attackDamage;
            abilityPower = defaultStats.abilityPower;
            armor = defaultStats.armor;
            currentWeapon = defaultStats.defaultWeapon;

            Debug.Log($"Statlar sýfýrlandý -> Gold: {gold}, Health: {CharHealth}, AttackDamage: {attackDamage}");
        }
        else
        {
            Debug.LogError("DefaultCharacterStats referansý atanmamýþ!");
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
            Debug.LogError("WeaponsScript referansý bulunamadý!");
            return;
        }
        weaponsScript.weaponData = weaponData;
        SpriteRenderer weaponRenderer = weaponObject.GetComponent<SpriteRenderer>();
        if (weaponRenderer != null && weaponData.weaponIcon != null)
        {
            weaponRenderer.sprite = weaponData.weaponIcon;
        }

        Debug.Log($"Yeni Silah Donatýldý: {weaponData.weaponName}");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            jumpCounter = 0;
            animator.ResetTrigger("isJumping");
        }
    }

    IEnumerator EnemyDamageOverTime(Enemies enemy)
    {
        isTakingDamage = true;
        while (enemy != null && currentHealth > 0 && isTakingDamage)
        {
            TakeDamage(enemy.enemyStats.enemyDamage);
            animator.SetTrigger("DamageTaken");
            Debug.Log($"Düþman hasar verdi: {enemy.enemyStats.enemyDamage}");
            yield return new WaitForSeconds(1f / enemy.enemyStats.attackSpeed);
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
