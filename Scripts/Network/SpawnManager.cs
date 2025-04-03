using UnityEngine;
using Unity.Netcode;

public class SpawnManager : NetworkBehaviour
{
    [Header("Spawn Points")]
    public Transform[] spawnPoints;  // Spawn noktaları dizisi

    private void Start()
    {
        if (IsServer)
        {
            // Server başladığında spawn noktalarını kontrol et
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("Spawn noktaları ayarlanmamış! Lütfen Inspector'dan spawn noktalarını ekleyin.");
            }
            else
            {
                Debug.Log($"Spawn Manager başlatıldı. {spawnPoints.Length} adet spawn noktası mevcut.");
                foreach (Transform point in spawnPoints)
                {
                    if (point == null)
                    {
                        Debug.LogError("Bir spawn noktası eksik! Lütfen tüm spawn noktalarını ayarlayın.");
                    }
                }
            }
        }
    }

    // Spawn noktasını al
    public Vector3 GetSpawnPoint(int playerIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn noktaları ayarlanmamış!");
            return Vector3.zero;
        }

        int index = playerIndex % spawnPoints.Length;
        return spawnPoints[index].position;
    }
} 