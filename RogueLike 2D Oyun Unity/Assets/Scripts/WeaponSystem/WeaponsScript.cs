using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class WeaponsScript : NetworkBehaviour
{
    public WeaponData weaponData;

    private Collider2D hitbox;
    private Animator animator;
    public Sprite rifleSprite;
    public Sprite pistolSprite;
    public GameObject riflePrefab;
    public GameObject pistolPrefab;
    public Animator weaponAnimator;
    public GameObject rifleBulletPrefab;
    public GameObject pistolBulletPrefab;
    public GameObject arrowPrefab;
    public Transform firePoint;
    public Enemies enemy;

    private bool canShoot = true;

    // Offline (local host) modunda olup olmadığımızı kontrol et
    private bool IsOfflineMode()
    {
        return GameManager.Instance != null && GameManager.Instance.isLocalHostMode;
    }

    void Start()
    {
        hitbox = GetComponent<Collider2D>();
        hitbox.enabled = false;
        animator = GetComponent<Animator>();
    }

    public int GetWeaponDamage()
    {
        if (weaponData != null)
        {
            return weaponData.damage;
        }
        return 0;
    }

    public float GetAttackSpeed()
    {
        if (weaponData != null)
        {
            return weaponData.attackSpeed;
        }
        return 1.0f;
    }

    public string GetWeaponName()
    {
        if (weaponData != null)
        {
            return weaponData.weaponName;
        }
        return "Bilinmeyen Silah";
    }

    public WeaponType GetWeaponType()
    {
        if (weaponData != null)
        {
            return weaponData.weaponType;
        }
        return WeaponType.Sword; 
    }

    public bool IsRangedWeapon()
    {
        if (weaponData != null)
        {
            return weaponData.weaponType == WeaponType.Bow || 
                   weaponData.weaponType == WeaponType.Rifle || 
                   weaponData.weaponType == WeaponType.Pistol;
        }
        return false;
    }

    public void Attack(int totalDamage)
    {
        if (weaponData == null) return;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, 1.5f);
        if (hit.collider != null)
        {
            Enemies enemy = hit.collider.GetComponent<Enemies>();
            if (enemy != null)
            {
                enemy.TakeDamageServerRpc(totalDamage);
            }
        }

        // Offline modda veya bu silahın sahibiyse saldırıyı gerçekleştir
        if (IsOfflineMode() || IsOwner)
        {
            PerformAttackServerRpc(totalDamage);
        }
    }
    
    [ServerRpc]
    void PerformAttackServerRpc(int damage, ServerRpcParams rpcParams = default)
    {
        DetectHit(damage);
    }

    void DetectHit(int damage)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, 1.5f);
        if (hit.collider != null)
        {
            Enemies enemy = hit.collider.GetComponent<Enemies>();
            if (enemy != null)
            {
                enemy.TakeDamageServerRpc(damage);
            }
        }
    }

    public void EnableHitbox()
    {
        hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        hitbox.enabled = false;
    }

    public void PlayAttackAnimation()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (weaponData == null) return;
        DeactivateAllWeapons();
        
        switch (weaponData.weaponType)
        {
            case WeaponType.Sword:
                animator.SetTrigger(weaponData.normalAttackTrigger);
                break;

            case WeaponType.Bow:
                hitbox.enabled = false;
                animator.SetTrigger(weaponData.normalAttackTrigger);
                Shoot();
                Attack(weaponData.damage);
                break;

            case WeaponType.Hammer:
                animator.SetTrigger(weaponData.normalAttackTrigger);
                break;

            case WeaponType.Scythe:
                animator.SetTrigger(weaponData.normalAttackTrigger);
                break;

            case WeaponType.Rifle:
                hitbox.enabled = false;
                ActivateWeapon(riflePrefab, weaponData.normalAttackTrigger);
                Shoot();
                Attack(weaponData.damage);
                break;

            case WeaponType.Pistol:
                hitbox.enabled = false;
                ActivateWeapon(pistolPrefab, weaponData.normalAttackTrigger);
                Attack(weaponData.damage);
                Shoot();
                break;

            default:
                Debug.LogWarning($"Bilinmeyen silah türü: {weaponData.weaponName}");
                break;
        }
    }
    
    private void ActivateWeapon(GameObject weaponPrefab, string animationTrigger)
    {
        if (weaponPrefab != null)
        {
            weaponPrefab.SetActive(true);
            animator.SetTrigger(animationTrigger);
        }
      
    }

    private void DeactivateAllWeapons()
    {
        if (riflePrefab != null) riflePrefab.SetActive(false);
        if (pistolPrefab != null) pistolPrefab.SetActive(false);
        
        Transform bowTransform = transform.parent.Find("Bow");
        if (bowTransform != null)
        {
            bowTransform.gameObject.SetActive(false);
        }
    }

    public void Shoot()
    {
        if (!canShoot || weaponData == null) return;

        // Offline modda veya bu silahın sahibiyse ateş et
        if (IsOfflineMode() || IsOwner)
        {
            ShootServerRpc();
            
            StartCoroutine(ShootCooldown());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShootServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!canShoot) return;
        
        canShoot = false;
        
        GameObject bulletPrefab = null;
        string weaponTypeStr = "Bilinmeyen";

        switch (weaponData.weaponType)
        {
            case WeaponType.Rifle:
                bulletPrefab = rifleBulletPrefab;
                weaponTypeStr = "Rifle";
                break;
            case WeaponType.Pistol:
                bulletPrefab = pistolBulletPrefab;
                weaponTypeStr = "Pistol";
                break;
            case WeaponType.Bow:
                bulletPrefab = arrowPrefab;
                weaponTypeStr = "Bow";
                break;
            default:
                Debug.LogWarning("Bu silah türü mermi atışı desteklemiyor.");
                canShoot = true;
                return;
        }

        if (bulletPrefab == null)
        {
            canShoot = true;
            return;
        }
        
        NetworkObject bulletNetworkObj = bulletPrefab.GetComponent<NetworkObject>();
        if (bulletNetworkObj == null)
        {
            canShoot = true;
            return;
        }

        // Get the client ID that requested the shot
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        // Find the character controller from the parent
        KarakterHareket characterController = transform.GetComponentInParent<KarakterHareket>();
        if (characterController == null || characterController.OwnerClientId != clientId)
        {
            Debug.LogWarning($"Client {clientId} attempted to shoot with a weapon they don't own!");
            canShoot = true;
            return;
        }
        
        ActivateWeaponClientRpc(weaponTypeStr);

        if (bulletPrefab != null && firePoint != null)
        {
            
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            
            Vector2 characterScale = transform.parent.localScale;
            Vector2 direction = characterScale.x > 0 ? Vector2.right : Vector2.left;
            
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                int totalDamage = CalculateWeaponDamage();
                bulletScript.Initialize(direction, totalDamage, transform.parent.gameObject);
                bulletScript.SetWeaponData(weaponData);
            }
            else
            { 
                Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = direction * 10f;
                }
            }
            
            NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
            if (bulletNetObj != null)
            {
                try 
                {
                    bulletNetObj.Spawn();
                }
                catch (System.Exception e)
                {
                    Destroy(bullet);
                    canShoot = true;
                    return;
                }
            }
            else
            {
                Destroy(bullet);
                canShoot = true;
                return;
            }

            TriggerWeaponAnimationClientRpc(weaponTypeStr);
        }
        else
        {
            canShoot = true;
        }
    }

    [ClientRpc]
    private void ActivateWeaponClientRpc(string weaponType)
    {
        DeactivateAllWeapons();
        
        GameObject weaponObj = null;
        
        switch (weaponType)
        {
            case "Rifle":
                if (riflePrefab != null) 
                {
                    weaponObj = riflePrefab;
                }
                break;
                
            case "Pistol":
                if (pistolPrefab != null) 
                {
                    weaponObj = pistolPrefab;
                }
                break;
                
            case "Bow":
                Transform bowTransform = transform.parent.Find("Bow");
                if (bowTransform != null)
                {
                    weaponObj = bowTransform.gameObject;
                }
                break;
        }
        
        if (weaponObj != null)
        {
            weaponObj.SetActive(true);
            
            SpriteRenderer renderer = weaponObj.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
                Color color = renderer.color;
                color.a = 1.0f;
                renderer.color = color;
            }
            
            StartCoroutine(DeactivateWeaponAfterDelay(weaponObj, 1.5f));
        }
    }

    private void PositionWeapon(GameObject weapon)
    {
        
            SpriteRenderer renderer = weapon.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 10; 
            }
            
        
    }

    private IEnumerator DeactivateWeaponAfterDelay(GameObject weapon, float delay)
    {
        if (weapon == null) yield break;
        
        yield return new WaitForSeconds(delay);
        
        if (weapon != null)
        {
            weapon.SetActive(false);
        }
    }

    public void OnWeaponChanged(WeaponData newWeaponData)
    {
        DeactivateAllWeapons();
        
        if (newWeaponData != null)
        {
            switch (newWeaponData.weaponType)
            {
                case WeaponType.Rifle:
                    if (riflePrefab != null)
                    {
                        PositionWeapon(riflePrefab);
                        riflePrefab.SetActive(true);
                    }
                    break;
                    
                case WeaponType.Pistol:
                    if (pistolPrefab != null)
                    {
                        PositionWeapon(pistolPrefab);
                        pistolPrefab.SetActive(true);
                    }
                    break;
                    
                case WeaponType.Bow:
                    Transform bowTransform = transform.parent.Find("Bow");
                    if (bowTransform != null)
                    {
                        PositionWeapon(bowTransform.gameObject);
                        bowTransform.gameObject.SetActive(true);
                    }
                    break;
                    
                default:
                    break;
            }
        }
    }

    public void SetWeaponData(WeaponData newWeaponData)
    {
        weaponData = newWeaponData;
        OnWeaponChanged(newWeaponData);
    }

    [ClientRpc]
    private void TriggerWeaponAnimationClientRpc(string weaponType)
    {
        string triggerName = "";
        
        if (weaponData != null && !string.IsNullOrEmpty(weaponData.normalAttackTrigger))
        {
            triggerName = weaponData.normalAttackTrigger;
        }
        else
        {
            switch (weaponType)
            {
                case "Rifle":
                    triggerName = "RifleAttack";
                    break;
                case "Pistol":
                    triggerName = "PistolAttack";
                    break;
                case "Bow":
                    triggerName = "ArrowAttack";
                    break;
                default:
                    triggerName = "Attack";
                    break;
            }
        }
        
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }

    private IEnumerator ShootCooldown()
    {
        canShoot = false;
        
        float cooldownTime = weaponData != null ? weaponData.attackSpeed : 1f;
        
        yield return new WaitForSeconds(cooldownTime);
        
        canShoot = true;
        
    }

    private int CalculateWeaponDamage()
    {
        return weaponData != null ? weaponData.damage : 0;
    }
}