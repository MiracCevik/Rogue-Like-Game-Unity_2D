using UnityEngine;
using Unity.Netcode;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    private NetworkVariable<Vector2> netVelocity = new NetworkVariable<Vector2>();
    private NetworkVariable<bool> netIsGrounded = new NetworkVariable<bool>();
    private Rigidbody2D rb;
    private bool isGrounded;
    private float moveInput;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        rb = GetComponent<Rigidbody2D>();
        SetupRigidbody();
    }

    private void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.gravityScale = 2f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.drag = 0.5f;
        }
    }

    private void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            JumpServerRpc();
        }

        if (moveInput != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(moveInput), 1, 1);
        }

        MoveServerRpc(moveInput);
    }

    [ServerRpc]
    private void MoveServerRpc(float input)
    {
        Vector2 newVelocity = new Vector2(input * moveSpeed, rb.velocity.y);
        rb.velocity = newVelocity;
        netVelocity.Value = newVelocity;
        
        // Tüm clientlara hareket bilgisini gönder
        UpdateMovementClientRpc(newVelocity);
    }

    [ClientRpc]
    private void UpdateMovementClientRpc(Vector2 velocity)
    {
        if (!IsOwner)
        {
            rb.velocity = velocity;
        }
    }

    [ServerRpc]
    private void JumpServerRpc()
    {
        if (netIsGrounded.Value)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            netIsGrounded.Value = false;
            JumpClientRpc();
        }
    }

    [ClientRpc]
    private void JumpClientRpc()
    {
        if (!IsOwner)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            if (IsServer)
            {
                netIsGrounded.Value = true;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            if (IsServer)
            {
                netIsGrounded.Value = false;
            }
        }
    }

    // Debug için görsel gösterim
    private void OnGUI()
    {
        if (IsSpawned)
        {
            Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(pos.x, Screen.height - pos.y, 100, 20), 
                     $"ID: {OwnerClientId} {(IsOwner ? "(You)" : "")}");
        }
    }
} 