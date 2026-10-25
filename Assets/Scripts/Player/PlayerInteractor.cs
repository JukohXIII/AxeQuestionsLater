using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    private IInteractable current;

    public void SetCurrent(IInteractable interactable) => current = interactable;

    public void ClearCurrent(IInteractable interactable)
    {
        if (current == interactable) current = null;
    }

    // Appelé par PlayerInput (Send Messages) pour l'action "Interact"
    void OnInteract(InputValue value)
    {
        if (value.isPressed && current != null) current.Interact();
    }
}