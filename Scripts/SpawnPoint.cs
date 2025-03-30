using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EnemySpawner : NetworkBehaviour
{
    public List<Transform> spawnPoints;
    public List<GameObject> enemyPrefabs;
    public int enemiesToSpawn = 10;
    private HashSet<Transform> usedSpawnPoints = new HashSet<Transform>();
    private NetworkVariable<int> networkEnemiesSpawned = new NetworkVariable<int>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnEnemies();
        }
    }

    void SpawnEnemies()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Transform spawnPoint = GetNextAvailableSpawnPoint();
            if (spawnPoint == null)
            {
                Debug.LogWarning("Tüm spawn noktalarý kullanýldý, yeni düþman oluþturulamýyor!");
                break;
            }

            GameObject randomEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            SpawnEnemyServerRpc(randomEnemy.name, spawnPoint.position);
            usedSpawnPoints.Add(spawnPoint);
            networkEnemiesSpawned.Value++;
        }
    }

    [ServerRpc]
    void SpawnEnemyServerRpc(string enemyPrefabName, Vector3 spawnPosition)
    {
        GameObject enemyPrefab = enemyPrefabs.Find(prefab => prefab.name == enemyPrefabName);
        if (enemyPrefab != null)
        {
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            enemy.SetActive(true);

            NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
        }
    }

    Transform GetNextAvailableSpawnPoint()
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (!usedSpawnPoints.Contains(spawnPoint))
            {
                return spawnPoint;
            }
        }

        Debug.LogWarning("Tüm noktalar dolu, rastgele nokta seçiliyor.");
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }
}
