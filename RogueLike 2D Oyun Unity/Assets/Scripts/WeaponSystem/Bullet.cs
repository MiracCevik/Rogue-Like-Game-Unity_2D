using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public WeaponData weaponData; 
    public float speed = 20f; 
    public float lifetime = 5f; 
    public Vector2 moveDirection; 
    private int bulletDamage;
    private GameObject owner;

    public bool isFacingRight = true;

    public void Initialize(Vector3 direction, int damage, GameObject bulletOwner)
    {
        moveDirection = new Vector2(direction.x, direction.y).normalized;
        bulletDamage = damage;
        owner = bulletOwner;
        
        if (direction.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1); 
            isFacingRight = false;
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1); 
            isFacingRight = true;
        }
    }

    public void SetWeaponData(WeaponData data)
    {
        weaponData = data;
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        if (direction.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1); 
            isFacingRight = false;
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1); 
            isFacingRight = true;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Network versiyonda mermi ömrünü sınırla
        if (IsServer)
        {
            Debug.Log($"Bullet: OnNetworkSpawn - pozisyon: {transform.position}, yön: {moveDirection}");
            Destroy(gameObject, lifetime);
        }
    }

    void Update()
    {
        // Her frame'de hareketi güncelle
        if (moveDirection != Vector2.zero)
        {
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
        }
        else
        {
            // Eğer moveDirection ayarlanmamışsa varsayılan hareket
            Vector3 defaultDirection = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
            transform.Translate(defaultDirection * speed * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        
        if (collision.CompareTag("Enemy"))
        {   
            Debug.Log("Mermi düşmana çarptı!");
            Enemies enemy = collision.GetComponent<Enemies>();
            if (enemy != null)
            {
                // Eğer bulletDamage tanımlanmış ise kullan, yoksa weaponData'dan al
                int damage = bulletDamage > 0 ? bulletDamage : (weaponData != null ? weaponData.damage : 10);
                enemy.TakeDamageServerRpc(damage);
                Debug.Log($"Düşmana {damage} hasar verildi!");
            }
            
            DestroyBullet();
        }
        else if (collision.CompareTag("Player"))
        {
            // Kendi sahip olduğu mermiden hasar alma
            if (collision.gameObject == owner) return;
            
            Debug.Log("Mermi oyuncuya çarptı!");
            KarakterHareket player = collision.GetComponent<KarakterHareket>();
            if (player != null)
            {
                // Eğer bulletDamage tanımlanmış ise kullan, yoksa weaponData'dan al
                int damage = bulletDamage > 0 ? bulletDamage : (weaponData != null ? weaponData.damage : 10);
                player.ReceiveDamageServerRpc(damage);
                Debug.Log($"Oyuncuya {damage} hasar verildi!");
            }
            
            DestroyBullet();
        }
        else if (collision.CompareTag("Ground") || IsDestructibleObject(collision))
        {
            Debug.Log($"Mermi {collision.gameObject.name} ile çarpıştı ve yok edildi.");
            DestroyBullet();
        }
    }

    // Merminin imha edilebileceği nesneleri kontrol et - tag kullanmadan
    private bool IsDestructibleObject(Collider2D collision)
    {
        // Nesnenin layer'ına göre kontrol et
        int layerIndex = collision.gameObject.layer;
        string layerName = LayerMask.LayerToName(layerIndex);
        
        // Çeşitli layer isimleri veya özelliklerine göre kontrol
        if (layerName.Contains("Ground") || 
            layerName.Contains("Wall") || 
            layerName.Contains("Obstacle") ||
            layerName.Contains("Environment"))
        {
            return true;
        }
        
        // Belli bileşenlere sahip nesneleri kontrol et
        if (collision.GetComponent<Rigidbody2D>() != null || 
            collision.GetComponent<Collider2D>() != null && !collision.isTrigger)
        {
            return true;
        }
        
        // Nesnenin ismine göre kontrol et
        string objName = collision.gameObject.name.ToLower();
        if (objName.Contains("wall") || 
            objName.Contains("ground") || 
            objName.Contains("platform") ||
            objName.Contains("obstacle") ||
            objName.Contains("block"))
        {
            return true;
        }
        
        return false;
    }

    // Mermiyi ağ üzerinden düzgün bir şekilde imha et
    private void DestroyBullet()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
