using UnityEngine;
using Unity.Netcode;

public class EnemyBullets : NetworkBehaviour
{
    public EnemyStats stats;
    public int bulletDamage;

    private void Start()
    {
        if (stats != null) bulletDamage = stats.enemyDamage;

        Destroy(gameObject, 4f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

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

                karakter.ReceiveDamageServerRpc(damageToApply);

                Destroy(gameObject);
            }
        }
    }
}
