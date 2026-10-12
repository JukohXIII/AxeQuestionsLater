using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactibleRadius = 1.5f;
    [SerializeField] private TMP_Text promptText;

    private PlayerInput playerInput;
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private IInteractable currentInteractable;
    private bool interactPressed;

    public bool Locked { get; private set; }

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    void Update()
    {
        currentInteractable = Locked ? null : FindClosest(); 

        if(promptText != null)
        {
            promptText.gameObject.SetActive(currentInteractable != null);
            if(currentInteractable != null) promptText.text = currentInteractable.Prompt;
        }

        if(interactPressed && currentInteractable != null && playerCombat.state == PlayerCombat.PlayerState.Normal)
            currentInteractable.Interact(this);
        
        interactPressed = false;
    }

    void OnInteract(InputValue value)
    {
       if (value.isPressed) interactPressed = true; 
    }

    private IInteractable FindClosest()
    {
        IInteractable bestCandidate = null;
        float bestDist = float.MaxValue;

        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, interactibleRadius))
        {
            var candidate = hit.GetComponentInParent<IInteractable>();
            if (candidate == null) continue;

            float distanceToCandidate = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distanceToCandidate < bestDist)
            {
                bestDist = distanceToCandidate;
                bestCandidate = candidate;
            }
        }
        return bestCandidate;
    }

    public void SetControlLocked(bool locked)
    {
        Locked = locked;
        if (locked)
        {
            playerInput.DeactivateInput();
            playerMovement.ClearInput();
            playerCombat.ClearInput();
        }
        else
        {
            playerInput.ActivateInput();
        }
    }
}