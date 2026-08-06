using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TMP_Text interactionText;

    [Header("Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayer;

    private IInteractable currentInteractable;

    private void Update()
    {
        DetectInteractable();
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed)
            return;

        currentInteractable?.Interact();
    }

    private void DetectInteractable()
    {
        currentInteractable = null;
        interactionText.gameObject.SetActive(false);

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance,
                interactionLayer
            ))
        {
            return;
        }

        currentInteractable =
            hit.collider.GetComponentInParent<IInteractable>();

        if (currentInteractable == null)
            return;

        interactionText.text =
            "[E] " + currentInteractable.GetInteractionText();

        interactionText.gameObject.SetActive(true);
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
            return;

        Gizmos.DrawRay(
            playerCamera.transform.position,
            playerCamera.transform.forward * interactionDistance
        );
    }
}