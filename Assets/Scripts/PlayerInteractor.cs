using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactibleRadius = 1.5f;
    [SerializeField] private TMP_Text promptText;
    [Tooltip("Gap (world units) between the top of the object and the prompt.")]
    [SerializeField] private float promptOffset = 0.3f;

    private PlayerInput playerInput;
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private IInteractable currentInteractable;
    private Collider2D currentCollider;
    private bool interactPressed;
    private Camera cam;

    public bool Locked { get; private set; }
    private string InteractKeyLabel =>
        playerInput.actions["Interact"].GetBindingDisplayString(group: playerInput.currentControlScheme);

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    void Update()
    {
        currentInteractable = Locked ? null : FindClosest(); 
        UpdatePrompt();

        if (interactPressed && currentInteractable != null
            && playerCombat.state == PlayerCombat.PlayerState.Normal)
        {
            currentInteractable.Interact(this);
        }
        
        interactPressed = false;
    }

    void OnInteract(InputValue value)
    {
       if (value.isPressed) interactPressed = true; 
    }

    private void UpdatePrompt()
    {
        if (promptText == null) return;

        bool show = currentInteractable != null;
        promptText.gameObject.SetActive(show);
        if (!show) return;

        promptText.text = $"[{InteractKeyLabel}] {currentInteractable.Prompt}";

        // Above the top of the object's collider, converted to screen coordinates
        if (cam == null) cam = Camera.main;
        Bounds bounds = currentCollider.bounds;
        Vector3 anchor = new Vector3(bounds.center.x, bounds.max.y + promptOffset, 0f);
        promptText.rectTransform.position = cam.WorldToScreenPoint(anchor);
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
                currentCollider = hit;
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