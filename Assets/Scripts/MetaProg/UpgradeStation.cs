using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class UpgradeStation : MonoBehaviour, IInteractable
{
    [SerializeField] private UpgradeCategory category;
    [SerializeField] private string title = "Upgrades";
    [SerializeField] private UpgradePanel panel;
    [Tooltip("Shown while the player is in range (e.g. an 'F' above the station).")]
    [SerializeField] private InteractionPrompt prompt;

    private PlayerInteractor interactorInRange;

    void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

    void Awake()
    {
        if (prompt != null) prompt.Hide();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only the collider on the player's root: ignores the attack hitbox and other child colliders
        var interactor = other.GetComponent<PlayerInteractor>();
        if (interactor == null) return;

        interactorInRange = interactor;
        interactor.SetCurrent(this);
        if (prompt != null) prompt.Show();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInteractor>() == null) return;
        Leave();
    }

    void OnDisable() => Leave();

    void Leave()
    {
        if (interactorInRange != null) interactorInRange.ClearCurrent(this);
        interactorInRange = null;
        if (prompt != null) prompt.Hide();
    }

    public void Interact() => panel.Open(category, title);
}