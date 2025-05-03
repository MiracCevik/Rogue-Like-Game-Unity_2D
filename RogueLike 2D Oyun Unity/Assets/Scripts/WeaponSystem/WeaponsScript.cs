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

    void Start()
    {   
        hitbox = GetComponent<Collider2D>();
        hitbox.enabled = false; 
        animator = GetComponent<Animator>();
        
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


        if (!IsOwner) return;

        PerformAttackServerRpc(totalDamage);
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
        switch (weaponData.weaponName)
        {
            case "Sword":
                animator.SetTrigger("SwordAttack");
                break;

            case "Bow":
                hitbox.enabled = false;
                animator.SetTrigger("BowAttack");
                Shoot();
                Attack(weaponData.damage);
                break;

            case "Hammer":
                animator.SetTrigger("HammerAttack");
                break;

            case "Scythe":
                animator.SetTrigger("ScytheAttack");
                break;

            case "Rifle":
                hitbox.enabled = false;
                ActivateWeapon(riflePrefab, "RifleAttack");
                Shoot();
                Attack(weaponData.damage);
                break;

            case "Pistol":
                hitbox.enabled = false;
                ActivateWeapon(pistolPrefab, "PistolAttack");
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
        else
        {
            Debug.LogWarning("Silah prefab'i atanmadý!");
        }
    }

    private void DeactivateAllWeapons()
    {
        if (riflePrefab != null) riflePrefab.SetActive(false);
        if (pistolPrefab != null) pistolPrefab.SetActive(false);
    }

    public float GetAttackSpeed()
    {
        return weaponData != null ? weaponData.attackSpeed : 1.0f; 
    }
    public void Shoot()
    {
        if (!canShoot || weaponData == null) return;

        GameObject bulletPrefab = null;

        switch (weaponData.weaponType)
        {
            case WeaponType.Rifle:
                bulletPrefab = rifleBulletPrefab;
                Attack(weaponData.damage);

                animator.SetTrigger("RifleAttack");
                Debug.Log("RifleAttack tetiklendi!");
                
                break;
            case WeaponType.Pistol:
                bulletPrefab = pistolBulletPrefab;
                Attack(weaponData.damage);
                animator.SetTrigger("PistolAttack"); 
                Debug.Log("PistolAttack tetiklendi!");
                break;
            case WeaponType.Bow:
                bulletPrefab = arrowPrefab;
                Attack(weaponData.damage);
                animator.SetTrigger("ArrowAttack");
                Debug.Log("ArrowAttack tetiklendi!");
                break;
            default:
                Debug.LogWarning("Bu silah mermi atýþý desteklemiyor.");
                return;
        }

        if (bulletPrefab != null && firePoint != null)
        {
          
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            Vector2 characterPosition = transform.position;
            Vector2 weaponPosition = firePoint.position;
            Vector2 direction = (weaponPosition.x - characterPosition.x) > 0 ? Vector2.right : Vector2.left;

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetDirection(direction);
                bulletScript.SetWeaponData(weaponData);
            }

            StartCoroutine(ShootCooldown());
        }


    }


    private IEnumerator ShootCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(weaponData.attackSpeed);
        canShoot = true;
    }

    private int CalculateWeaponDamage()
    {
        return weaponData != null ? weaponData.damage : 0;
    }
   
}
