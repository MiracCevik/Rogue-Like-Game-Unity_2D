using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class ChestSpawnManager : NetworkBehaviour
{
    [Header("Spawn Ayarları")]
    public GameObject chestPrefab;
    [SerializeField] private Transform[] spawnPoints;
    public float minSpawnTime = 30f;
    public float maxSpawnTime = 60f;
    public int maxActiveChests = 3;

    private List<GameObject> activeChests = new List<GameObject>();
    private List<bool> occupiedSpawnPoints = new List<bool>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        occupiedSpawnPoints.Clear();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            occupiedSpawnPoints.Add(false);
        }
        
        StartCoroutine(SpawnChestRoutine());
    }

    private IEnumerator SpawnChestRoutine()
    {
        while (true)
        {
            CleanupInactiveChests();

            if (activeChests.Count < maxActiveChests)
            {
                int availablePointIndex = GetRandomAvailableSpawnPointIndex();
                if (availablePointIndex >= 0)
                {
                    SpawnChest(availablePointIndex);
                }
            }

            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void CleanupInactiveChests()
    {
        activeChests.RemoveAll(chest => chest == null);
        
        for (int i = 0; i < occupiedSpawnPoints.Count; i++)
        {
            occupiedSpawnPoints[i] = false;
        }
        
        foreach (GameObject chest in activeChests)
        {
            if (chest == null) continue;
            
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (Vector3.Distance(chest.transform.position, spawnPoints[i].position) < 0.1f)
                {
                    occupiedSpawnPoints[i] = true;
                    break;
                }
            }
        }
    }

    private int GetRandomAvailableSpawnPointIndex()
    {
        List<int> availableIndices = new List<int>();
        
        for (int i = 0; i < occupiedSpawnPoints.Count; i++)
        {
            if (!occupiedSpawnPoints[i])
            {
                availableIndices.Add(i);
            }
        }
        
        if (availableIndices.Count == 0) return -1;
        
        return availableIndices[Random.Range(0, availableIndices.Count)];
    }

    private void SpawnChest(int spawnPointIndex)
    {
        if (!IsServer) return;

        GameObject chest = Instantiate(chestPrefab, spawnPoints[spawnPointIndex].position, Quaternion.identity);
        NetworkObject netObj = chest.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();
            activeChests.Add(chest);
            occupiedSpawnPoints[spawnPointIndex] = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;
            
            bool isOccupied = false;
            if (occupiedSpawnPoints != null && i < occupiedSpawnPoints.Count)
            {
                isOccupied = occupiedSpawnPoints[i];
            }
            
            Gizmos.color = isOccupied ? Color.red : Color.green;
            Gizmos.DrawWireSphere(spawnPoints[i].position, 0.5f);
        }
    }
}