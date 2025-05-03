using UnityEngine;

public class Bullet : MonoBehaviour
{
    public WeaponData weaponData; 
    public float speed = 20f; 
    public float lifetime = 5f; 
    public Vector2 moveDirection; 
    private SpriteRenderer spriteRenderer;

    public bool isFacingRight = true;

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


    void Start()
    {
        Destroy(gameObject, lifetime); 
    }

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D collision1)
    {
        if (collision1.CompareTag("Enemy"))
        {   
            Debug.Log("Mermi düþmana çarptý!");
            Enemies enemy = collision1.GetComponent<Enemies>();
            if (enemy != null)
            {
                enemy.TakeDamageServerRpc(weaponData.damage);
                Debug.Log("Düþmana hasar verildi!");
            }
            Destroy(gameObject);
        }
    }

}
