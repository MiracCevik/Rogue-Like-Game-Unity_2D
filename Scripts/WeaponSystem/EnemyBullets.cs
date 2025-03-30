using UnityEngine;

public class EnemyBullets : MonoBehaviour
{
    public EnemyStats stats;
    private void Start()
    {
        Destroy(gameObject, 4f);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            KarakterHareket karakter = collision.GetComponent<KarakterHareket>();

            karakter.TakeDamage(stats.enemyDamage);
            Debug.Log("Player'a hasar verildi: " + stats.enemyDamage);

            Destroy(gameObject);
        }

    }


}
