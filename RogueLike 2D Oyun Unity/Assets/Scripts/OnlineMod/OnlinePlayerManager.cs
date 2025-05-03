using UnityEngine;
using Unity.Netcode;

public class OnlinePlayerManager : NetworkBehaviour
{
    private KarakterHareket characterController;
    private Rigidbody2D rb;
    private NetworkVariable<int> networkHealth = new NetworkVariable<int>();

    private void Awake()
    {
        characterController = GetComponent<KarakterHareket>();
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] OnNetworkSpawn START for object {gameObject.name}. IsOwner: {IsOwner}");

        if (IsOwner)
        {
            Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] OnNetworkSpawn -> IsOwner block entered.");
            // Local player setup
            Camera.main.GetComponent<CameraFollow>()?.SetTarget(transform);
            
            Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] OnNetworkSpawn -> Enabling CharacterController. Current state: {characterController?.enabled}");
            characterController.enabled = true;

            Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] OnNetworkSpawn -> Checking Rigidbody. rb is null? {(rb == null)}");
            if (rb != null) 
            {
                Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] OnNetworkSpawn -> Setting rb.isKinematic to false. Current value: {rb.isKinematic}");
                rb.isKinematic = false;
                // Kinematic durumunu AYARLADIKTAN HEMEN SONRA loglayalım
                Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] OnNetworkSpawn -> Rigidbody Kinematic SET TO: {rb.isKinematic}"); 
            }
            else
            {
                Debug.LogError($"[{NetworkManager.Singleton.LocalClientId}] OnNetworkSpawn -> Rigidbody is NULL! Cannot set kinematic state.");
            }
        }
        else
        {
            Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] OnNetworkSpawn -> IsOwner=False block entered for object owned by {OwnerClientId}. Disabling CharacterController.");
            // Remote player setup
            characterController.enabled = false;
        }
        Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] OnNetworkSpawn END for object {gameObject.name}.");
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int damage)
    {
        networkHealth.Value -= damage;
        UpdateHealthClientRpc(networkHealth.Value);
    }

    [ClientRpc]
    private void UpdateHealthClientRpc(int newHealth)
    {
        if (!IsOwner && characterController != null)
        {
            // KarakterHareket'in TakeDamage'i health'i azaltıyor, RPC ile tekrar çağırmak yanlış olabilir.
            // TODO: Sağlık güncellemesini NetworkVariable OnValueChanged ile yapmak daha iyi olabilir.
        }
    }
} 