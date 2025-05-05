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

    // WeaponData'dan silah hasarını al
    public int GetWeaponDamage()
    {
        if (weaponData != null)
        {
            return weaponData.damage;
        }
        return 0;
    }

    // WeaponData'dan saldırı hızını al
    public float GetAttackSpeed()
    {
        if (weaponData != null)
        {
            return weaponData.attackSpeed;
        }
        return 1.0f;
    }

    // WeaponData'dan silah adını al
    public string GetWeaponName()
    {
        if (weaponData != null)
        {
            return weaponData.weaponName;
        }
        return "Bilinmeyen Silah";
    }

    // WeaponData'dan silah tipini al
    public WeaponType GetWeaponType()
    {
        if (weaponData != null)
        {
            return weaponData.weaponType;
        }
        return WeaponType.Sword; // Varsayılan olarak kılıç
    }

    // Silahın uzak menzilli olup olmadığını kontrol et
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
        
        // Silah tipine göre uygun animasyonu oynat ve silahı aktifleştir
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
        else
        {
            Debug.LogWarning("Silah prefab'i atanmadý!");
        }
    }

    private void DeactivateAllWeapons()
    {
        if (riflePrefab != null) riflePrefab.SetActive(false);
        if (pistolPrefab != null) pistolPrefab.SetActive(false);
        
        // Bow nesnesini bul ve devre dışı bırak
        Transform bowTransform = transform.parent.Find("Bow");
        if (bowTransform != null)
        {
            bowTransform.gameObject.SetActive(false);
        }
    }

    public void Shoot()
    {
        if (!canShoot || weaponData == null) return;

        if (IsOwner)
        {
            ShootServerRpc();
            
            // Cooldown'ı başlatmak için beklemeden hemen ShootCooldown'ı çağır
            StartCoroutine(ShootCooldown());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShootServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!canShoot) return;
        
        // ShootServerRpc içindeki canShoot kontrolden sonra 
        // canShoot'u false yap, böylece server tarafında da kontrol edilmiş olur
        canShoot = false;
        
        GameObject bulletPrefab = null;
        string weaponTypeStr = "Bilinmeyen";

        // Weapon tipine göre uygun mermi prefabını seç
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
                // CanShoot'u true yap ki tekrar deneyebilsin
                canShoot = true;
                return;
        }

        // Bullet prefab var mı kontrol et
        if (bulletPrefab == null)
        {
            Debug.LogError($"Server: {weaponTypeStr} için bulletPrefab NULL! Lütfen prefabı atayın.");
            // CanShoot'u true yap ki tekrar deneyebilsin
            canShoot = true;
            return;
        }
        
        // NetworkObject bileşeni var mı kontrol et
        NetworkObject bulletNetworkObj = bulletPrefab.GetComponent<NetworkObject>();
        if (bulletNetworkObj == null)
        {
            Debug.LogError($"Server: {weaponTypeStr} bulletPrefab'ında NetworkObject bileşeni yok! Lütfen prefaba NetworkObject ekleyin.");
            // CanShoot'u true yap ki tekrar deneyebilsin  
            canShoot = true;
            return;
        }

        // Silahı tüm istemcilerde göster
        ActivateWeaponClientRpc(weaponTypeStr);

        if (bulletPrefab != null && firePoint != null)
        {
            // FirePoint pozisyonu doğru mu kontrol et
            Debug.Log($"Server: firePoint pozisyonu: {firePoint.position}, rotasyon: {firePoint.rotation}");
            
            // Mermiyi oluştur
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            
            // Doğru yön ve hareketi sağla
            Vector2 characterScale = transform.parent.localScale;
            Vector2 direction = characterScale.x > 0 ? Vector2.right : Vector2.left;
            
            // Bullet bileşeni varsa kullan
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                int totalDamage = CalculateWeaponDamage();
                bulletScript.Initialize(direction, totalDamage, transform.parent.gameObject);
                bulletScript.SetWeaponData(weaponData);
                Debug.Log($"Server: Mermi başlatıldı. Yön: {direction}, Hasar: {totalDamage}");
            }
            else
            {
                Debug.LogError($"Server: Mermi nesnesinde Bullet bileşeni bulunamadı!");
                // Basit hareket ekle
                Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = direction * 10f;
                    Debug.Log($"Server: Alternatif hareket eklendi - yön: {direction}");
                }
            }
            
            // Ağ üzerinde spawn et
            NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
            if (bulletNetObj != null)
            {
                try 
                {
                    bulletNetObj.Spawn();
                    Debug.Log($"Server: {weaponTypeStr} mermisi ağda spawn edildi.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Server: Mermi spawn edilirken hata: {e.Message}");
                    Destroy(bullet);
                    // Hata olduğunda CanShoot'u true yap ki tekrar deneyebilsin
                    canShoot = true;
                    return;
                }
            }
            else
            {
                Debug.LogError($"Server: {weaponTypeStr} mermisinde NetworkObject bileşeni bulunamadı!");
                Destroy(bullet);
                // Hata olduğunda CanShoot'u true yap ki tekrar deneyebilsin
                canShoot = true;
                return;
            }

            TriggerWeaponAnimationClientRpc(weaponTypeStr);
        }
        else
        {
            Debug.LogError($"Server: bulletPrefab veya firePoint null! bulletPrefab:{bulletPrefab}, firePoint:{firePoint}");
            // Hata olduğunda CanShoot'u true yap ki tekrar deneyebilsin
            canShoot = true;
        }
    }

    [ClientRpc]
    private void ActivateWeaponClientRpc(string weaponType)
    {
        // Önce tüm silahları devre dışı bırak
        DeactivateAllWeapons();
        
        // Sadece ateşlenen silahı aktifleştir
        GameObject weaponObj = null;
        
        switch (weaponType)
        {
            case "Rifle":
                if (riflePrefab != null) 
                {
                    weaponObj = riflePrefab;
                    Debug.Log($"Client: Rifle silahı aktifleştiriliyor. GameObject: {riflePrefab.name}");
                }
                break;
                
            case "Pistol":
                if (pistolPrefab != null) 
                {
                    weaponObj = pistolPrefab;
                    Debug.Log($"Client: Pistol silahı aktifleştiriliyor. GameObject: {pistolPrefab.name}");
                }
                break;
                
            case "Bow":
                Transform bowTransform = transform.parent.Find("Bow");
                if (bowTransform != null)
                {
                    weaponObj = bowTransform.gameObject;
                    Debug.Log($"Client: Bow silahı aktifleştiriliyor. GameObject: {bowTransform.name}");
                }
                break;
        }
        
        if (weaponObj != null)
        {
            // Silahı aktifleştir, pozisyon ve ölçeğe dokunma
            weaponObj.SetActive(true);
            
            // Silahın görüntülenmesini garanti etmek için
            SpriteRenderer renderer = weaponObj.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
                Color color = renderer.color;
                // Tamamen görünür olmasını sağla
                color.a = 1.0f;
                renderer.color = color;
            }
            
            Debug.Log($"Client: {weaponType} silahı aktifleştirildi.");
            StartCoroutine(DeactivateWeaponAfterDelay(weaponObj, 1.5f));
        }
        else
        {
            Debug.LogWarning($"Client: {weaponType} silahı bulunamadı veya aktifleştirilemedi!");
        }
    }

    // Silahı karakterin elinde doğru pozisyona yerleştir
    private void PositionWeapon(GameObject weapon)
    {
        try
        {
            // NOT: Kullanıcının isteği üzerine pozisyon ve ölçek değiştirilmiyor
            // Prefab'ın orijinal pozisyonu ve ölçeği korunuyor
            
            // Sadece üst katmanda göstermek için sırasını ayarla
            SpriteRenderer renderer = weapon.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 10; // Karakterin önünde görünmesi için yüksek bir değer
            }
            
            Debug.Log($"Client: Silah aktifleştirildi.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Client: Silah aktifleştirilirken hata oluştu: {e.Message}");
        }
    }

    // Silahı gecikmeyle devre dışı bırak
    private IEnumerator DeactivateWeaponAfterDelay(GameObject weapon, float delay)
    {
        if (weapon == null) yield break;
        
        yield return new WaitForSeconds(delay);
        
        if (weapon != null)
        {
            weapon.SetActive(false);
            Debug.Log($"Client: Silah devre dışı bırakıldı: {weapon.name}");
        }
    }

    // Mevcut silah değiştiğinde çağrılan metod
    public void OnWeaponChanged(WeaponData newWeaponData)
    {
        // Önce tüm silahları devre dışı bırak
        DeactivateAllWeapons();
        
        // Yeni silah tipine göre uygun silahı aktifleştir
        if (newWeaponData != null)
        {
            switch (newWeaponData.weaponType)
            {
                case WeaponType.Rifle:
                    if (riflePrefab != null)
                    {
                        PositionWeapon(riflePrefab);
                        riflePrefab.SetActive(true);
                        Debug.Log($"Silah değişimi: Rifle aktifleştirildi");
                    }
                    break;
                    
                case WeaponType.Pistol:
                    if (pistolPrefab != null)
                    {
                        PositionWeapon(pistolPrefab);
                        pistolPrefab.SetActive(true);
                        Debug.Log($"Silah değişimi: Pistol aktifleştirildi");
                    }
                    break;
                    
                case WeaponType.Bow:
                    Transform bowTransform = transform.parent.Find("Bow");
                    if (bowTransform != null)
                    {
                        PositionWeapon(bowTransform.gameObject);
                        bowTransform.gameObject.SetActive(true);
                        Debug.Log($"Silah değişimi: Bow aktifleştirildi");
                    }
                    break;
                    
                default:
                    // Diğer silah tipleri için gerekirse ekle
                    break;
            }
        }
    }

    // WeaponData değiştiğinde bu metodu çağır
    public void SetWeaponData(WeaponData newWeaponData)
    {
        weaponData = newWeaponData;
        OnWeaponChanged(newWeaponData);
    }

    [ClientRpc]
    private void TriggerWeaponAnimationClientRpc(string weaponType)
    {
        string triggerName = "";
        
        // WeaponData'daki normalAttackTrigger'ı kullan
        if (weaponData != null && !string.IsNullOrEmpty(weaponData.normalAttackTrigger))
        {
            triggerName = weaponData.normalAttackTrigger;
        }
        else
        {
            // Fallback olarak silah tipine göre belirle
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
            Debug.Log($"Client: {triggerName} animasyonu tetiklendi.");
        }
        else
        {
            Debug.LogError($"Client: Animator bulunamadı, {triggerName} animasyonu tetiklenemedi!");
        }
    }

    private IEnumerator ShootCooldown()
    {
        // canShoot'u false yap - şimdi client tarafında başlatılıyor
        canShoot = false;
        
        // AttackSpeed doğrudan atışlar arasındaki süre olarak yorumla (saniye cinsinden)
        // Bu değer ne kadar küçükse, atış hızı o kadar yüksektir
        float cooldownTime = weaponData != null ? weaponData.attackSpeed : 1f;
        
        // Bekleme süresi sonunda ateş edebilmeyi etkinleştir
        yield return new WaitForSeconds(cooldownTime);
        
        // Süre sonunda tekrar ateş edebilir
        canShoot = true;
        
        Debug.Log($"Ateş cooldown bitti ({cooldownTime}s). Tekrar ateş edilebilir.");
    }

    private int CalculateWeaponDamage()
    {
        return weaponData != null ? weaponData.damage : 0;
    }
}