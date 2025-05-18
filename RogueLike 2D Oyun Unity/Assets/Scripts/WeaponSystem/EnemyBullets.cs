using UnityEngine;
using Unity.Netcode;

public class EnemyBullets : NetworkBehaviour
{
    public EnemyStats stats;
    public int bulletDamage;
    public Vector2 moveDirection;
    public float speed = 10f;

    private void Start()
    {
        if (stats != null) bulletDamage = stats.enemyDamage;

        Destroy(gameObject, 4f);
    }

    private void Update()
    {
        // Eğer yön atanmışsa onu kullan
        if (moveDirection != Vector2.zero)
        {
            transform.Translate(moveDirection * speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // In single player mode, we still want to process damage
        bool isOfflineMode = GameManager.Instance != null && GameManager.Instance.isLocalHostMode;
        
        // Only check IsServer in online mode
        if (!isOfflineMode && !IsServer) return;

        if (collision.CompareTag("Player"))
        {
            KarakterHareket karakter = collision.GetComponent<KarakterHareket>();
            if (karakter != null)
            {
                int damageToApply = bulletDamage > 0 ? bulletDamage : (stats != null ? stats.enemyDamage : 0);
                if (damageToApply <= 0)
                {
                    return;
                }

                karakter.TakeDamage(damageToApply);

                Destroy(gameObject);
            }
        }
        else if (collision.CompareTag("Ground") || IsWall(collision))
        {
            Destroy(gameObject);
        }
    }

    private bool IsWall(Collider2D collision)
    {
        string layerName = LayerMask.LayerToName(collision.gameObject.layer);
        return layerName.Contains("Ground") || layerName.Contains("Wall") || 
               layerName.Contains("Obstacle") || layerName.Contains("Environment");
    }
}
