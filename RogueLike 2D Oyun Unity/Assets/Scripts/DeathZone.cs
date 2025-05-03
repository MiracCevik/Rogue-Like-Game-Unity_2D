using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathZone : MonoBehaviour
{
    public KarakterHareket karakterHareket;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            karakterHareket.Die();
            SceneManager.LoadScene("Level1");

        }
        if (other.gameObject != gameObject)
        {
            Destroy(other.gameObject);
            Debug.Log(other.gameObject.name + " yok edildi.");
        }
        
    }
}
