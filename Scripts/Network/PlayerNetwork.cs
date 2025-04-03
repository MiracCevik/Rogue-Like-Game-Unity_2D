using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Combat")]
    public WeaponsScript weaponsScript;
    public GameObject weaponObject;
    public float attackSpeed = 1.0f;
    private bool canAttack = true;
    public Animator animator;
    public NetworkAnimator networkAnimator;

    private NetworkVariable<float> netXScale = new NetworkVariable<float>(1f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner);
    
    private NetworkVariable<Vector2> netVelocity = new NetworkVariable<Vector2>();
    private NetworkVariable<bool> netIsGrounded = new NetworkVariable<bool>();
    private NetworkVariable<bool> netIsAttacking = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private NetworkVariable<int> netAttackDamage = new NetworkVariable<int>();
    
    private Rigidbody2D rb;
    private bool isGrounded;
    private float moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 2f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.drag = 0.5f;
        }

        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        if (weaponObject != null)
        {
            weaponsScript = weaponObject.GetComponent<WeaponsScript>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        netXScale.OnValueChanged += OnScaleChanged;
        netVelocity.OnValueChanged += OnVelocityChanged;
        netIsAttacking.OnValueChanged += OnAttackStateChanged;

        // Debug için
        Debug.Log($"[{(IsHost ? "HOST" : "CLIENT")}] Network Spawn - IsOwner: {IsOwner}, HasAnimator: {animator != null}, HasNetworkAnimator: {networkAnimator != null}");
    }

    public override void OnNetworkDespawn()
    {
        netXScale.OnValueChanged -= OnScaleChanged;
        netVelocity.OnValueChanged -= OnVelocityChanged;
        netIsAttacking.OnValueChanged -= OnAttackStateChanged;
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Input alma
        moveInput = Input.GetAxisRaw("Horizontal");

        // Zıplama
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            JumpServerRpc();
        }

        // Karakter yönünü değiştirme
        if (moveInput != 0)
        {
            float newXScale = Mathf.Sign(moveInput);
            transform.localScale = new Vector3(newXScale, 1, 1);
            if (netXScale.Value != newXScale)
            {
                netXScale.Value = newXScale;
            }
        }

        // Saldırı kontrolü
        if (Input.GetButtonDown("Fire1") && canAttack)
        {
            Debug.Log($"[{(IsHost ? "HOST" : "CLIENT")}] Attempting to attack - HasAnimator: {animator != null}, HasWeaponScript: {weaponsScript != null}");
            PlayLocalAttackAnimation();
            AttackServerRpc();
        }

        // Hareket komutunu server'a gönder
        MoveServerRpc(moveInput);
    }

    private void PlayLocalAttackAnimation()
    {
        Debug.Log($"[{(IsHost ? "HOST" : "CLIENT")}] Entering PlayLocalAttackAnimation");
        
        if (animator == null)
        {
            Debug.LogError($"[{(IsHost ? "HOST" : "CLIENT")}] Animator is null!");
            return;
        }

        if (weaponsScript == null)
        {
            Debug.LogError($"[{(IsHost ? "HOST" : "CLIENT")}] WeaponScript is null!");
            return;
        }

        Debug.Log($"[{(IsHost ? "HOST" : "CLIENT")}] Playing local attack animation");
        animator.SetTrigger(weaponsScript.weaponData.normalAttackTrigger);
        animator.SetBool("isAttacking", true);
        
        if (networkAnimator != null)
        {
            Debug.Log($"[{(IsHost ? "HOST" : "CLIENT")}] Triggering NetworkAnimator");
            networkAnimator.SetTrigger(weaponsScript.weaponData.normalAttackTrigger);
        }
        
        StartCoroutine(LocalAttackAnimation());
    }

    private IEnumerator LocalAttackAnimation()
    {
        if (weaponsScript != null)
        {
            weaponsScript.EnableHitbox();
            weaponsScript.PlayAttackAnimation();
        }

        yield return new WaitForSeconds(attackSpeed);

        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }
        if (weaponsScript != null)
        {
            weaponsScript.DisableHitbox();
        }
    }

    [ServerRpc]
    private void MoveServerRpc(float input)
    {
        Vector2 newVelocity = new Vector2(input * moveSpeed, rb.velocity.y);
        rb.velocity = newVelocity;
        netVelocity.Value = newVelocity;

        // Animasyon kontrolü
        if (animator != null)
        {
            animator.SetBool("isRunning", input != 0);
        }
    }

    [ServerRpc]
    private void JumpServerRpc()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;
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

    [ServerRpc]
    private void AttackServerRpc()
    {
        Debug.Log($"[SERVER] Attack request received");
        if (weaponsScript != null && canAttack)
        {
            netIsAttacking.Value = true;
            netAttackDamage.Value = weaponsScript.weaponData.damage;
            StartCoroutine(PerformAttack());
            AttackClientRpc();
        }
    }

    private IEnumerator PerformAttack()
    {
        canAttack = false;
        if (weaponsScript != null)
        {
            weaponsScript.EnableHitbox();
            if (animator != null && !IsOwner)
            {
                animator.SetTrigger(weaponsScript.weaponData.normalAttackTrigger);
                animator.SetBool("isAttacking", true);
                if (networkAnimator != null)
                {
                    networkAnimator.SetTrigger(weaponsScript.weaponData.normalAttackTrigger);
                }
            }
            weaponsScript.PlayAttackAnimation();
            weaponsScript.Attack(weaponsScript.weaponData.damage);
        }

        yield return new WaitForSeconds(attackSpeed);
        canAttack = true;
        netIsAttacking.Value = false;
        if (animator != null && !IsOwner)
        {
            animator.SetBool("isAttacking", false);
        }
        if (weaponsScript != null)
        {
            weaponsScript.DisableHitbox();
        }
    }

    [ClientRpc]
    private void AttackClientRpc()
    {
        Debug.Log($"[{(IsHost ? "HOST" : "CLIENT")}] Attack RPC received");
        if (!IsOwner && weaponsScript != null)
        {
            StartCoroutine(ClientAttackCoroutine());
        }
    }

    private IEnumerator ClientAttackCoroutine()
    {
        Debug.Log($"[{(IsHost ? "HOST" : "CLIENT")}] Starting attack coroutine");
        if (weaponsScript != null)
        {
            weaponsScript.EnableHitbox();
            if (animator != null)
            {
                animator.SetTrigger(weaponsScript.weaponData.normalAttackTrigger);
                animator.SetBool("isAttacking", true);
                if (networkAnimator != null)
                {
                    networkAnimator.SetTrigger(weaponsScript.weaponData.normalAttackTrigger);
                }
            }
            weaponsScript.PlayAttackAnimation();
        }

        yield return new WaitForSeconds(attackSpeed);
        
        if (weaponsScript != null)
        {
            weaponsScript.DisableHitbox();
        }
        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }
    }

    private void OnScaleChanged(float previousValue, float newValue)
    {
        transform.localScale = new Vector3(newValue, 1, 1);
    }

    private void OnVelocityChanged(Vector2 previousValue, Vector2 newValue)
    {
        if (!IsOwner)
        {
            rb.velocity = newValue;
        }
    }

    private void OnAttackStateChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"[{(IsHost ? "HOST" : "CLIENT")}] Attack state changed: {newValue}");
        if (!IsOwner && weaponsScript != null && animator != null)
        {
            if (newValue)
            {
                weaponsScript.EnableHitbox();
                animator.SetTrigger(weaponsScript.weaponData.normalAttackTrigger);
                animator.SetBool("isAttacking", true);
                if (networkAnimator != null)
                {
                    networkAnimator.SetTrigger(weaponsScript.weaponData.normalAttackTrigger);
                }
            }
            else
            {
                weaponsScript.DisableHitbox();
                animator.SetBool("isAttacking", false);
            }
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