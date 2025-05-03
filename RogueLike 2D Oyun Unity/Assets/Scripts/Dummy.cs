using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dummy : MonoBehaviour
{
    public int maxHealth = 200;
    private int currentHealth;

    public Image greenHealthBar;
    public Image redHealthBar;

    private float lastDamageTime;
    private bool isTakingDamage = false;
    public float regenDelay = 3f;
    public AttackRange attackRangeScript;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    void Update()
    {
        if (isTakingDamage && Time.time - lastDamageTime >= regenDelay)
        {
            currentHealth = maxHealth;
            UpdateHealthBar();
            Debug.Log("Can maxHealth'a yeniden ayarlandý.");
            isTakingDamage = false; 
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Çarpýþan nesne: {other.name}, Tag: {other.tag}");

        if (other.CompareTag("Weapon"))
        {
            Debug.Log("Weapon ile çarpýþma tetiklendi.");
            currentHealth -= 40;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthBar();
            Debug.Log("Can:" + currentHealth);

            lastDamageTime = Time.time;
            isTakingDamage = true;
        }

        if (other.CompareTag("Bullet"))
        {
            Debug.Log("Bullet ile çarpýþma tetiklendi.");
            Bullet bulletScript = other.GetComponent<Bullet>();
            if (bulletScript != null && bulletScript.weaponData != null)
            {
                int bulletDamage = bulletScript.weaponData.damage;
                currentHealth -= bulletDamage; 
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                UpdateHealthBar();
                Debug.Log("Can:" + currentHealth);
            }

            Destroy(other.gameObject);
            lastDamageTime = Time.time;
            isTakingDamage = true;
        }
    }


    void UpdateHealthBar()
    {
        float healthRatio = (float)currentHealth / maxHealth;
        greenHealthBar.rectTransform.localScale = new Vector3(healthRatio, 1, 1);
        redHealthBar.enabled = currentHealth < maxHealth;
    }
}
