using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactDistance = 4.0f;
    [SerializeField] private LayerMask interactableLayer = ~0;
    [SerializeField] private Transform cameraTransform;

    private IInteractable currentInteractable;
    private IInteractable currentGrabbedInteractable; // Elde tutulan obje

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        if (cameraTransform == null) return;

        Vector3 rayOrigin = cameraTransform.position + (cameraTransform.forward * 0.3f);
        Ray ray = new Ray(rayOrigin, cameraTransform.forward);

        Debug.DrawRay(rayOrigin, cameraTransform.forward * interactDistance, currentInteractable != null ? Color.green : Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                currentInteractable = interactable;
                return;
            }
        }

        currentInteractable = null;
    }

    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            // 1. Eğer elimizde halihazırda tuttuğumuz bir obje varsa -> Onu bırak
            if (currentGrabbedInteractable != null)
            {
                currentGrabbedInteractable.OnStopInteract(gameObject);
                currentGrabbedInteractable = null;
                Debug.Log("<color=yellow>[Bırakıldı]</color> Obje serbest bırakıldı.");
                return;
            }

            // 2. Eğer elimiz boşsa ve baktığımız yerde bir obje varsa -> Onu al
            if (currentInteractable != null)
            {
                currentGrabbedInteractable = currentInteractable;
                currentInteractable.OnInteract(gameObject);
                Debug.Log($"<color=green>[Tetiklendi]</color> {currentInteractable.GetInteractPrompt()}");
            }
        }
    }
}