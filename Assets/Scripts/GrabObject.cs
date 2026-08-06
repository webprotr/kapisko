using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrabObject : MonoBehaviour, IInteractable
{
    [Header("Grab Settings")]
    [SerializeField] private string objectName = "Nesne";

    protected Rigidbody rb;
    protected bool isGrabbed = false;
    private Collider objectCollider;
    private GameObject currentInteractor;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        objectCollider = GetComponent<Collider>();
    }

    public virtual string GetInteractPrompt()
    {
        return isGrabbed ? $"[E] {objectName}'i Bırak" : $"[E] {objectName}'i Al";
    }

    public virtual void OnInteract(GameObject interactor)
    {
        currentInteractor = interactor;

        if (!isGrabbed)
        {
            Grab(interactor);
        }
        else
        {
            OnStopInteract(interactor);
        }
    }

    protected virtual void Grab(GameObject interactor)
    {
        PlayerHands hands = interactor.GetComponentInChildren<PlayerHands>();
        Collider playerCollider = interactor.GetComponent<Collider>();

        if (hands != null)
        {
            isGrabbed = true;
            rb.isKinematic = true;

            // Karakter ile Objenin Collider'ları arasındaki çarpışmayı kapat
            if (objectCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(objectCollider, playerCollider, true);
            }

            hands.Grab(this.transform);
        }
    }

    public virtual void OnStopInteract(GameObject interactor)
    {
        PlayerHands hands = interactor.GetComponentInChildren<PlayerHands>();
        Collider playerCollider = interactor.GetComponent<Collider>();

        if (hands != null)
        {
            isGrabbed = false;

            // Objenin kendisini elden çıkar
            transform.SetParent(null);

            // Fiziği tekrar aktif et
            rb.isKinematic = false;

            // Karakter ile çarpışmayı tekrar aç
            if (objectCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(objectCollider, playerCollider, false);
            }

            hands.Release();
        }
    }
}