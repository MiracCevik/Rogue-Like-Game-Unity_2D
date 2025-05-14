using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class ChestSpawnManager : NetworkBehaviour
{
    [System.Serializable]
    public class SpawnPoint
    {
        public Vector3 position;
        public bool isOccupied;
    }

    [Header("Spawn Ayarları")]
    public GameObject chestPrefab;
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    public float minSpawnTime = 30f;
    public float maxSpawnTime = 60f;
    public int maxActiveChests = 3;

    private List<GameObject> activeChests = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(SpawnChestRoutine());
    }

    private IEnumerator SpawnChestRoutine()
    {
        while (true)
        {
            CleanupInactiveChests();

            if (activeChests.Count < maxActiveChests)
            {
                SpawnPoint availablePoint = GetRandomAvailableSpawnPoint();
                if (availablePoint != null)
                {
                    SpawnChest(availablePoint);
                }
            }

            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void CleanupInactiveChests()
    {
        activeChests.RemoveAll(chest => chest == null);
        
        foreach (SpawnPoint point in spawnPoints)
        {
            bool hasChestAtPoint = false;
            foreach (GameObject chest in activeChests)
            {
                if (chest != null && Vector3.Distance(chest.transform.position, point.position) < 0.1f)
                {
                    hasChestAtPoint = true;
                    break;
                }
            }
            point.isOccupied = hasChestAtPoint;
        }
    }

    private SpawnPoint GetRandomAvailableSpawnPoint()
    {
        List<SpawnPoint> availablePoints = spawnPoints.FindAll(p => !p.isOccupied);
        
        if (availablePoints.Count == 0) return null;
        
        return availablePoints[Random.Range(0, availablePoints.Count)];
    }

    private void SpawnChest(SpawnPoint spawnPoint)
    {
        if (!IsServer) return;

        GameObject chest = Instantiate(chestPrefab, spawnPoint.position, Quaternion.identity);
        NetworkObject netObj = chest.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();
            activeChests.Add(chest);
            spawnPoint.isOccupied = true;
        }
    }

    private void OnDrawGizmos()
    {
        foreach (SpawnPoint point in spawnPoints)
        {
            Gizmos.color = point.isOccupied ? Color.red : Color.green;
            Gizmos.DrawWireSphere(point.position, 0.5f);
        }
    }
}