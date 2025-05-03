using UnityEngine;
using Unity.Netcode;

public class EnemyBullets : NetworkBehaviour
{
    public EnemyStats stats;
    public int bulletDamage;

    private void Start()
    {
        if (stats != null) bulletDamage = stats.enemyDamage;
        else Debug.LogWarning("EnemyBullets: EnemyStats not assigned, using default damage.");

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
                    Debug.LogWarning($"[Server] Enemy bullet hit player {karakter.OwnerClientId} but damage is zero.");
                    return;
                }

                Debug.Log($"[Server] Enemy bullet hit player {karakter.OwnerClientId}. Applying {damageToApply} damage via RPC.");
                karakter.ReceiveDamageServerRpc(damageToApply);

                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("[Server] Enemy bullet hit something tagged Player, but it has no KarakterHareket script.");
            }
        }
    }
}
