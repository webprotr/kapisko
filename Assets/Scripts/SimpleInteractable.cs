using UnityEngine;

public class SimpleInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionText = "Etkileşim";

    public string GetInteractionText()
    {
        return interactionText;
    }

    public void Interact()
    {
        Debug.Log(gameObject.name + " ile etkileşime girildi.");
    }
}