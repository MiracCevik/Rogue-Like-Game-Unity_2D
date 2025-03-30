using UnityEngine;

public class AoEAttackHitbox : MonoBehaviour
{
    public int damage = 50;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Çarpýþan nesne: {collision.name}");
        if (collision.CompareTag("Enemy"))
        {
            Debug.Log($"Çarpýþan nesnenin tag'i: {collision.tag}");

            Enemies enemy = collision.GetComponent<Enemies>();
            if (enemy != null)
            {
                enemy.TakeDamageServerRpc(damage);
                Debug.Log($"{collision.name} AoE saldýrýsýndan {damage} hasar aldý!");
            }
           
        }
    }

}
