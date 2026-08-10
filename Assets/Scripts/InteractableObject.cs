using System.Collections;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransformUnreliable))]
public class InteractableObject : NetworkBehaviour
{
    public string itemName = "Prop";

    [SyncVar] public bool isHeld = false;
    [SyncVar] public PlayerController ownerPlayer;

    [Header("Combat / Damage Settings")]
    [SerializeField] private float minDamageSpeed = 2.5f; // Hasar verme eşiği
    [SerializeField] private float damageAmount = 25f;    // Çarpma anında verilecek hasar miktarı

    private Rigidbody rb;
    private Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // Yüksek hızlı tünelleme / zeminden düşme sorununu engeller
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // --- OBJEYİ ELE ALMA ---
    public void OnPickedUp(Transform holdSocket, PlayerController player)
    {
        CmdOnPickedUp(holdSocket != null ? holdSocket.GetComponentInParent<NetworkIdentity>() : null, player != null ? player.netIdentity : null);
    }

    [Command(requiresAuthority = false)]
    private void CmdOnPickedUp(NetworkIdentity socketNetId, NetworkIdentity playerNetId)
    {
        isHeld = true;
        if (playerNetId != null) ownerPlayer = playerNetId.GetComponent<PlayerController>();

        RpcOnPickedUp(socketNetId);
    }

    [ClientRpc]
    private void RpcOnPickedUp(NetworkIdentity socketNetId)
    {
        isHeld = true;
        rb.isKinematic = true;
        col.enabled = false;

        if (socketNetId != null)
        {
            transform.SetParent(socketNetId.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    // --- OBJEYİ BIRAKMA ---
    public void OnDropped()
    {
        CmdOnDropped();
    }

    [Command(requiresAuthority = false)]
    private void CmdOnDropped()
    {
        isHeld = false;
        RpcOnDropped();
    }

    [ClientRpc]
    private void RpcOnDropped()
    {
        isHeld = false;
        transform.SetParent(null);

        rb.isKinematic = false;
        col.enabled = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // --- OBJEYİ FIRLATMA ---
    public void Throw(Vector3 spawnPosition, Vector3 force, Collider playerCollider)
    {
        PlayerController thrower = ownerPlayer;
        CmdThrow(spawnPosition, force, thrower != null ? thrower.netIdentity : null);
    }

    [Command(requiresAuthority = false)]
    private void CmdThrow(Vector3 spawnPosition, Vector3 force, NetworkIdentity throwerNetId)
    {
        isHeld = false;
        if (throwerNetId != null) ownerPlayer = throwerNetId.GetComponent<PlayerController>();

        transform.position = spawnPosition;
        transform.SetParent(null);

        rb.isKinematic = false;
        col.enabled = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
        #else
            rb.velocity = Vector3.zero;
        #endif

        rb.AddForce(force, ForceMode.Impulse);

        RpcOnThrow(spawnPosition, force);
    }

    [ClientRpc]
    private void RpcOnThrow(Vector3 spawnPosition, Vector3 force)
    {
        isHeld = false;
        transform.position = spawnPosition;
        transform.SetParent(null);

        rb.isKinematic = false;
        col.enabled = true;

        #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
        #else
            rb.velocity = Vector3.zero;
        #endif

        rb.AddForce(force, ForceMode.Impulse);
    }

    private IEnumerator ResetCollisionWithPlayer(Collider playerCol, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (col != null && playerCol != null)
        {
            Physics.IgnoreCollision(col, playerCol, false);
        }
    }

    // --- ÇARPIŞMA VE HASAR HESAPLAMASI (SUNUCUDA ÇALIŞIR) ---
    private void OnCollisionEnter(Collision collision)
    {
        // Hasar ve Fırlatılan Obje Hesabını Çiftleme Yaşanmaması İçin Sadece Sunucu (Host) Yapar
        if (!isServer) return;

        #if UNITY_6000_0_OR_NEWER
            float currentSpeed = rb != null ? rb.linearVelocity.magnitude : 0f;
        #else
            float currentSpeed = rb != null ? rb.velocity.magnitude : 0f;
        #endif

        if (!isHeld && currentSpeed > minDamageSpeed)
        {
            PlayerController hitPlayer = collision.gameObject.GetComponent<PlayerController>();

            if (hitPlayer != null && hitPlayer != ownerPlayer)
            {
                // 1. Çarptığı oyuncuya HASAR ver
                hitPlayer.TakeDamage(damageAmount);

                // 2. Çarptığı oyuncunun ELİNDEKİ EŞYAYI düşür
                hitPlayer.DropItem();

                // 3. İsabet ettikten sonra hızını kır ve sahibini temizle
                #if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity *= 0.2f;
                #else
                    rb.velocity *= 0.2f;
                #endif

                ownerPlayer = null;
            }
        }
    }
}