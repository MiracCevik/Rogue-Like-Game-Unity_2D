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
    
    private bool IsOfflineMode()
    {
        return GameManager.Instance != null && GameManager.Instance.isLocalHostMode;
    }

    void Start()
    {
        // Offline modda (veya NetworkManager yoksa) doğrudan SpawnEnemies çağır
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode())
        {
            Debug.Log("Offline modda düşmanlar spawn ediliyor...");
            SpawnEnemiesForOfflineMode();
        }
    }

    public override void OnNetworkSpawn()
    {
        // Online modda ve sunucu isek spawn et
        if (IsServer && !IsOfflineMode())
        {
            Debug.Log("Online modda düşmanlar spawn ediliyor...");
            SpawnEnemies();
        }
    }

    // Offline mod için spawn metodu - NetworkObject spawn etmeden yapılır
    void SpawnEnemiesForOfflineMode()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Transform spawnPoint = GetNextAvailableSpawnPoint();
            if (spawnPoint == null)
            {
                Debug.LogWarning("Yeterli spawn noktası yok!");
                break;
            }

            // Rastgele düşman seç
            GameObject randomEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            
            // Offline mod için doğrudan instantiate et
            Vector3 adjustedPosition = new Vector3(spawnPoint.position.x, spawnPoint.position.y + 0.5f, spawnPoint.position.z);
            GameObject enemy = Instantiate(randomEnemy, adjustedPosition, Quaternion.identity);
            enemy.SetActive(true);
            
            // Düşmanı başlat
            InitializeEnemyInstance(enemy);
            
            usedSpawnPoints.Add(spawnPoint);
        }
    }

    void SpawnEnemies()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Transform spawnPoint = GetNextAvailableSpawnPoint();
            if (spawnPoint == null)
            {
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
            // Adjust position to account for spawn on ground
            Vector3 adjustedPosition = new Vector3(spawnPosition.x, spawnPosition.y + 0.5f, spawnPosition.z);
            
            GameObject enemy = Instantiate(enemyPrefab, adjustedPosition, Quaternion.identity);
            enemy.SetActive(true);

            // Make sure enemy is properly initialized
            InitializeEnemyInstance(enemy);

            NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
        }
    }

    private void InitializeEnemyInstance(GameObject enemy)
    {
        Enemies enemyComponent = enemy.GetComponent<Enemies>();
        if (enemyComponent == null) return;
        
        // Make sure enemy stats are initialized
        if (enemyComponent.enemyStats == null)
        {
            Debug.LogWarning("Enemy prefab missing EnemyStats reference!");
            return;
        }
        
        // Explicitly set health values
        enemyComponent.currentHealth = enemyComponent.enemyStats.enemyHealth;
        enemyComponent.currentDamage = enemyComponent.enemyStats.enemyDamage;
        enemyComponent.currentAS = enemyComponent.enemyStats.attackSpeed;
        
        // Make sure health bars are properly assigned and visible
        if (enemyComponent.greenHealthBar != null && enemyComponent.redHealthBar != null)
        {
            enemyComponent.greenHealthBar.gameObject.SetActive(true);
            enemyComponent.redHealthBar.gameObject.SetActive(true);
            enemyComponent.UpdateHealthBar();
        }
        else
        {
            Debug.LogWarning("Enemy prefab missing health bar references!");
        }
        
        // Ensure animator is assigned
        if (enemyComponent.animator == null)
        {
            enemyComponent.animator = enemy.GetComponent<Animator>();
        }
        
        // Ensure attack range is assigned
        AttackRange attackRange = enemy.GetComponentInChildren<AttackRange>();
        if (attackRange != null)
        {
            attackRange.stats = enemyComponent.enemyStats;
            
            // Find the player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                attackRange.player = playerObj.transform;
                enemyComponent.player = playerObj.transform;
                enemyComponent.karakterRef = playerObj.GetComponent<KarakterHareket>();
                
                // İlk başlangıçta düşmanın oyuncuya dönük olmasını sağla
                if (playerObj.transform.position.x < enemy.transform.position.x)
                {
                    // Oyuncu solda, düşman sola baksın
                    Vector3 scale = enemy.transform.localScale;
                    scale.x = -Mathf.Abs(scale.x); // Orijinal boyutu koru, sadece yönü değiştir
                    enemy.transform.localScale = scale;
                }
                else
                {
                    // Oyuncu sağda, düşman sağa baksın
                    Vector3 scale = enemy.transform.localScale;
                    scale.x = Mathf.Abs(scale.x); // Orijinal boyutu koru, sadece yönü değiştir
                    enemy.transform.localScale = scale;
                }
            }
        }
        
        // Set base Z position for proper sorting
        Vector3 position = enemy.transform.position;
        position.z = 0;
        enemy.transform.position = position;
        
        // Düşmanın attack behavior'unu hemen başlat
        enemyComponent.AssignAttackBehavior();
    }

    Transform GetNextAvailableSpawnPoint()
    {
        // Get all available spawn points that are not used yet
        List<Transform> availableSpawnPoints = new List<Transform>();
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (!usedSpawnPoints.Contains(spawnPoint))
            {
                availableSpawnPoints.Add(spawnPoint);
            }
        }
        
        // If there are available spawn points, choose one randomly
        if (availableSpawnPoints.Count > 0)
        {
            return availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
        }
        
        // If all spawn points are used, reset and pick a random one
        if (usedSpawnPoints.Count >= spawnPoints.Count)
        {
            Debug.LogWarning("Tüm noktalar dolu, rastgele nokta seçiliyor ve kullanılan noktalar sıfırlanıyor.");
            usedSpawnPoints.Clear();
            return spawnPoints[Random.Range(0, spawnPoints.Count)];
        }
        
        return null;
    }
}
