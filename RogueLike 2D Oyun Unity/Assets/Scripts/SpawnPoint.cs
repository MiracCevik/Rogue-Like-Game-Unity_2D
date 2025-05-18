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
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsOfflineMode())
        {
            SpawnEnemiesForOfflineMode();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && !IsOfflineMode())
        {
            SpawnEnemies();
        }
    }
    void SpawnEnemiesForOfflineMode()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Transform spawnPoint = GetNextAvailableSpawnPoint();
            if (spawnPoint == null)
            {
                break;
            }

            GameObject randomEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            Vector3 adjustedPosition = new Vector3(spawnPoint.position.x, spawnPoint.position.y + 0.5f, spawnPoint.position.z);
            GameObject enemy = Instantiate(randomEnemy, adjustedPosition, Quaternion.identity);
            enemy.SetActive(true);
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
            Vector3 adjustedPosition = new Vector3(spawnPosition.x, spawnPosition.y + 0.5f, spawnPosition.z);
            
            GameObject enemy = Instantiate(enemyPrefab, adjustedPosition, Quaternion.identity);
            enemy.SetActive(true);

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
        
        if (enemyComponent.enemyStats == null)
        {
            return;
        }
        
        enemyComponent.currentHealth = enemyComponent.enemyStats.enemyHealth;
        enemyComponent.currentDamage = enemyComponent.enemyStats.enemyDamage;
        enemyComponent.currentAS = enemyComponent.enemyStats.attackSpeed;
        
        if (enemyComponent.greenHealthBar != null && enemyComponent.redHealthBar != null)
        {
            enemyComponent.greenHealthBar.gameObject.SetActive(true);
            enemyComponent.redHealthBar.gameObject.SetActive(true);
            enemyComponent.UpdateHealthBar();
        }
     
        if (enemyComponent.animator == null)
        {
            enemyComponent.animator = enemy.GetComponent<Animator>();
        }
        AttackRange attackRange = enemy.GetComponentInChildren<AttackRange>();
        if (attackRange != null)
        {
            attackRange.stats = enemyComponent.enemyStats;
            
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                attackRange.player = playerObj.transform;
                enemyComponent.player = playerObj.transform;
                enemyComponent.karakterRef = playerObj.GetComponent<KarakterHareket>();
                
                if (playerObj.transform.position.x < enemy.transform.position.x)
                {
                    Vector3 scale = enemy.transform.localScale;
                    scale.x = -Mathf.Abs(scale.x); 
                    enemy.transform.localScale = scale;
                }
                else
                {
                    Vector3 scale = enemy.transform.localScale;
                    scale.x = Mathf.Abs(scale.x);
                    enemy.transform.localScale = scale;
                }
            }
        }
        
        Vector3 position = enemy.transform.position;
        position.z = 0;
        enemy.transform.position = position;
        
        enemyComponent.AssignAttackBehavior();
    }

    Transform GetNextAvailableSpawnPoint()
    {
        List<Transform> availableSpawnPoints = new List<Transform>();
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (!usedSpawnPoints.Contains(spawnPoint))
            {
                availableSpawnPoints.Add(spawnPoint);
            }
        }
        
        if (availableSpawnPoints.Count > 0)
        {
            return availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
        }
        
        if (usedSpawnPoints.Count >= spawnPoints.Count)
        {
            usedSpawnPoints.Clear();
            return spawnPoints[Random.Range(0, spawnPoints.Count)];
        }
        
        return null;
    }
}
