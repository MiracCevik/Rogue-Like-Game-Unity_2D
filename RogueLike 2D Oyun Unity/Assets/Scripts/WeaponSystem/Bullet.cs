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

    public NetworkVariable<bool> isFacingRight = new NetworkVariable<bool>(true);

    public NetworkVariable<Vector2> netDirection = new NetworkVariable<Vector2>();

    public void Initialize(Vector3 direction, int damage, GameObject bulletOwner)
    {
        moveDirection = new Vector2(direction.x, direction.y).normalized;
        bulletDamage = damage;
        owner = bulletOwner;
        if (IsServer)
        {
            netDirection.Value = moveDirection;
            isFacingRight.Value = direction.x >= 0;
        }
    }

    public void SetWeaponData(WeaponData data)
    {
        weaponData = data;
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        if (IsServer)
        {
            isFacingRight.Value = direction.x >= 0;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            Destroy(gameObject, lifetime);
        }
    }

    void Update()
    {
        Vector2 dir = netDirection.Value != Vector2.zero ? netDirection.Value : moveDirection;
        transform.Translate(dir * speed * Time.deltaTime, Space.World);
        
        transform.localScale = new Vector3(isFacingRight.Value ? 1 : -1, 1, 1);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // In single player mode, we still want to process damage
        bool isOfflineMode = GameManager.Instance != null && GameManager.Instance.isLocalHostMode;
        
        // Only check IsServer in online mode
        if (!isOfflineMode && !IsServer) return;
        
        if (collision.CompareTag("Enemy"))
        {   
            Enemies enemy = collision.GetComponent<Enemies>();
            if (enemy != null)
            {
                int damage = bulletDamage > 0 ? bulletDamage : (weaponData != null ? weaponData.damage : 10);
                
                // In offline mode, call directly the damage method if available
                if (isOfflineMode && enemy.GetType().GetMethod("TakeDamage") != null)
                {
                    enemy.SendMessage("TakeDamage", damage);
                }
                else
                {
                    enemy.TakeDamageServerRpc(damage);
                }
            }
            
            DestroyBullet();
        }
        else if (collision.CompareTag("Player"))
        {
            if (collision.gameObject == owner) return;
            
            KarakterHareket player = collision.GetComponent<KarakterHareket>();
            if (player != null)
            {
                int damage = bulletDamage > 0 ? bulletDamage : (weaponData != null ? weaponData.damage : 10);
                player.TakeDamage(damage);
            }
            
            DestroyBullet();
        }
        else if (collision.CompareTag("Ground") || IsDestructibleObject(collision))
        {
            DestroyBullet();
        }
    }

    private bool IsDestructibleObject(Collider2D collision)
    {
        int layerIndex = collision.gameObject.layer;
        string layerName = LayerMask.LayerToName(layerIndex);
        
        if (layerName.Contains("Ground") || 
            layerName.Contains("Wall") || 
            layerName.Contains("Obstacle") ||
            layerName.Contains("Environment"))
        {
            return true;
        }
        
        if (collision.GetComponent<Rigidbody2D>() != null || 
            collision.GetComponent<Collider2D>() != null && !collision.isTrigger)
        {
            return true;
        }
        
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
