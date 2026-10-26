using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionPrompt : MonoBehaviour
{
    // Réglages (en code, pas dans l'Inspector)
    private const float IconHeight = 0.6f;   
    private const string ActionName = "Interact";
    private const float Duration = 0.2f;
    private const float SlideDistance = 0.3f; 
    private const int SortingOrder = 100;

    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private ControlGlyphs glyphs;
    [Tooltip("Optionnel : texte affiché si aucun sprite n'existe pour ce contrôle.")]
    [SerializeField] private TextMeshPro fallbackLabel;

    private PlayerInput playerInput;
    private Vector3 shownPosition;
    private bool anchored;
    private Color iconColor = Color.white;
    private float visibility;
    private float target;

    void Awake()
    {
        if (!anchored) shownPosition = transform.localPosition;
        iconColor = icon.color;
        icon.sortingOrder = SortingOrder;
        if (fallbackLabel != null) fallbackLabel.sortingOrder = SortingOrder + 1;
        RefreshGlyph();
        Apply();
    }

    void OnEnable()
    {
        InputDeviceTracker.Changed += RefreshGlyph;
        RefreshGlyph();
    }

    void OnDisable() => InputDeviceTracker.Changed -= RefreshGlyph;

    public void SetAnchor(Vector3 worldPosition)
    {
        anchored = true;
        transform.position = worldPosition;
        shownPosition = transform.localPosition;
        icon.transform.localPosition = Vector3.zero;
        Apply();
    }

    public void Show()
    {
        RefreshGlyph();
        // AudioManager.Play(Sfx.PromptPop);
        target = 1f;
    }

    public void Hide() => target = 0f;

    void Update()
    {
        if (Mathf.Approximately(visibility, target)) return;
        visibility = Mathf.MoveTowards(visibility, target, Time.unscaledDeltaTime / Duration);
        Apply();
    }

    void RefreshGlyph()
    {
        if (playerInput == null) playerInput = FindFirstObjectByType<PlayerInput>();
        InputAction action = playerInput != null ? playerInput.actions.FindAction(ActionName) : null;

        Sprite sprite = null;
        string text = "?";

        var family = InputDeviceTracker.Current;
        if (action != null && glyphs != null
            && BindingGlyph.TryGet(action, family, out string control, out string display))
        {
            sprite = glyphs.Get(family, control);
            text = display;
        }

        icon.sprite = sprite;
        icon.enabled = sprite != null;

        if (sprite != null)
        {
            float spriteHeight = sprite.rect.height / sprite.pixelsPerUnit;

            Vector3 parentScale = icon.transform.parent != null ? icon.transform.parent.lossyScale : Vector3.one;
            float s = IconHeight / spriteHeight;
            icon.transform.localScale = new Vector3(s / parentScale.x, s / parentScale.y, 1f);
        }

        if (fallbackLabel != null)
        {
            fallbackLabel.text = text;
            fallbackLabel.enabled = sprite == null;
        }
    }

    void Apply()
    {
        float eased = Mathf.SmoothStep(0f, 1f, visibility);
        transform.localPosition = shownPosition + (1f - eased) * SlideDistance * Vector3.down;

        Color c = iconColor;
        c.a = iconColor.a * eased;
        icon.color = c;

        if (fallbackLabel != null)
        {
            Color t = fallbackLabel.color;
            t.a = eased;
            fallbackLabel.color = t;
        }
    }
}