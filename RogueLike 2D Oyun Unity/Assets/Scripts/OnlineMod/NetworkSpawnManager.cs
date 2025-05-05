using UnityEngine;
using Unity.Netcode;

public class NetworkSpawnManager : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += SpawnPlayers;
            NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
        }
    }

    void SpawnPlayers()
    {
    }

    void SpawnPlayer(ulong clientId)
    {
        if (IsServer)
        {
            Transform spawnPoint = GetRandomSpawnPoint();
            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerObject != null)
            {
                playerObject.transform.position = spawnPoint.position;
                var rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
        }
    }

    Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return transform; 
        }
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnPlayers;
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
        }
    }
} 