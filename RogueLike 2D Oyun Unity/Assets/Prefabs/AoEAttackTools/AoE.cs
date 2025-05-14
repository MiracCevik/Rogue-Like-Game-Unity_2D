using UnityEngine;

public class AoEAttackHitbox : MonoBehaviour
{
    public int damage = 50;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {

            Enemies enemy = collision.GetComponent<Enemies>();
            if (enemy != null)
            {
                enemy.TakeDamageServerRpc(damage);
            }
           
        }
    }

}
