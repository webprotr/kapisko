using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class InteractableObject : MonoBehaviour
{
    public string itemName = "Prop";
    public bool isHeld = false;
    public PlayerController ownerPlayer;

    [Header("Combat / Damage Settings")]
    [SerializeField] private float minDamageSpeed = 2.5f; // Hasar 
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

    public void OnPickedUp(Transform holdSocket, PlayerController player)
    {
        isHeld = true;
        ownerPlayer = player;

        rb.isKinematic = true;
        col.enabled = false;

        transform.SetParent(holdSocket);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void OnDropped()
    {
        isHeld = false;

        transform.SetParent(null);
        rb.isKinematic = false;
        col.enabled = true;

        // Bırakıldığında da tünelleme korumasını açık tut
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Throw(Vector3 spawnPosition, Vector3 force, Collider playerCollider)
    {
        // Not: ownerPlayer bilgisini fırlatma anında koruyoruz ki atışı yapan kişi kendine vurmasın
        PlayerController thrower = ownerPlayer;

        OnDropped();
        ownerPlayer = thrower; // Fırlatan oyuncuyu çarpışma anına kadar tutuyoruz

        transform.position = spawnPosition;

        if (playerCollider != null && col != null)
        {
            Physics.IgnoreCollision(col, playerCollider, true);
            StartCoroutine(ResetCollisionWithPlayer(playerCollider, 0.5f));
        }

        rb.linearVelocity = Vector3.zero;
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

    private void OnCollisionEnter(Collision collision)
    {
        // Obje elde tutulmuyorsa ve belirlenen hızın üzerinde fırlatıldıysa
        if (!isHeld && rb != null && rb.linearVelocity.magnitude > minDamageSpeed)
        {
            PlayerController hitPlayer = collision.gameObject.GetComponent<PlayerController>();

            // Çarptığı kişi fırlatan oyuncunun kendisi değilse
            if (hitPlayer != null && hitPlayer != ownerPlayer)
            {
                // 1. Çarptığı oyuncuya HASAR ver (Can düşer ve GetHit animasyonu tetiklenir)
                hitPlayer.TakeDamage(damageAmount);

                // 2. Çarptığı oyuncunun ELİNDEKİ EŞYAYI düşür
                hitPlayer.DropItem();

                // 3. İsabet ettikten sonra objenin hızını kırıp sahibini sıfırla (üst üste hasar vurmasın)
                rb.linearVelocity *= 0.2f;
                ownerPlayer = null;
            }
        }
    }
}