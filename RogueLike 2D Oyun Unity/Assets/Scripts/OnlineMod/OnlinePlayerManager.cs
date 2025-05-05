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

        if (IsOwner)
        {
            Camera.main.GetComponent<CameraFollow>()?.SetTarget(transform);
            
            characterController.enabled = true;

            if (rb != null) 
            {
                rb.isKinematic = false;
            }
         
        }
        else
        {
            characterController.enabled = false;
        }
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
       
    }
} 