using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class NetworkPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    private int nextSpawnPointIndex = 0;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnServerStarted()
    {
        if (IsServer)
        {
            // Host oyuncusunu spawn et
            SpawnPlayer(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer && clientId != NetworkManager.Singleton.LocalClientId)
        {
            // Yeni bağlanan client'ı spawn et
            SpawnPlayer(clientId);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;

        StartCoroutine(SpawnPlayerDelayed(clientId));
    }

    private IEnumerator SpawnPlayerDelayed(ulong clientId)
    {
        // Network bağlantısının tamamen kurulması için kısa bir bekleme
        yield return new WaitForSeconds(0.5f);

        var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (player != null)
        {
            Vector3 spawnPosition = GetNextSpawnPosition();
            
            // Önce fizik motorunu devre dışı bırak
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = false;
            }

            // Pozisyonu ayarla
            player.transform.position = spawnPosition;

            // Fizik değerlerini sıfırla ve fizik motorunu tekrar etkinleştir
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = true;
            }
        }
    }

    public Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Vector3[] defaultPositions = new Vector3[]
            {
                new Vector3(-2f, 2f, 0f), // Y pozisyonunu biraz yükselttim
                new Vector3(2f, 2f, 0f),
                new Vector3(-4f, 2f, 0f),
                new Vector3(4f, 2f, 0f)
            };

            int index = nextSpawnPointIndex % defaultPositions.Length;
            nextSpawnPointIndex++;
            return defaultPositions[index];
        }

        int pointIndex = nextSpawnPointIndex % spawnPoints.Length;
        nextSpawnPointIndex++;
        return spawnPoints[pointIndex].position;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
} 