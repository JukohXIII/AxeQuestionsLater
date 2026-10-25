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

    void Awake()
    {
        var pi = GetComponent<PlayerInput>();
        foreach (var b in pi.actions["Interact"].bindings)
            pi.actions["Interact"].performed += ctx => Debug.Log("[Interact] action Interact performed");
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            var pi = GetComponent<PlayerInput>();
            Debug.Log($"[Interact] F brut | inputIsActive={pi.inputIsActive} | action.enabled={pi.actions["Interact"].enabled} | timeScale={Time.timeScale}", this);
        }
    }
}