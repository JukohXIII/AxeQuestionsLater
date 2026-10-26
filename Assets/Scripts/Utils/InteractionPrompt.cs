using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;
    [SerializeField] private SpriteRenderer background;

    [Header("Animation")]
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private float slideDistance = 0.3f;

    [Header("Look")]
    [SerializeField] private Vector2 padding = new(0.25f, 0.12f);
    [SerializeField, Range(0f, 1f)] private float backgroundAlpha = 0.75f;
    [SerializeField] private int sortingOrder = 100;

    private Vector3 shownPosition;
    private Color textColor;
    private Color backgroundColor;
    private float visibility;
    private float target;

    void Awake()
    {
        shownPosition = transform.localPosition;
        textColor = label.color;
        backgroundColor = background.color;

        background.sortingOrder = sortingOrder;
        label.sortingOrder = sortingOrder + 1;

        FitBackground();
        Apply();
    }

    public void Show()
    {
        target = 1f;
    }
    public void Hide() => target = 0f;

    public void SetText(string text)
    {
        label.text = text;
        FitBackground();
    }

    void Update()
    {
        if (Mathf.Approximately(visibility, target)) return;
        visibility = Mathf.MoveTowards(visibility, target, Time.unscaledDeltaTime/duration);
        Apply();
    }

    void Apply()
    {
        float eased = Mathf.SmoothStep(0f, 1f, visibility);

        transform.localPosition = shownPosition + (1f - eased) * slideDistance * Vector3.down;

        Color text = textColor;  text.a = textColor.a * eased;   label.color = text;
        Color b = backgroundColor; b.a = backgroundAlpha * eased; background.color = b;
    }

    void FitBackground()
    {
        label.ForceMeshUpdate();
        Bounds bounds = label.textBounds;

        background.drawMode = SpriteDrawMode.Sliced;
        background.size = (Vector2)bounds.size + padding * 2f;
        background.transform.position = label.transform.TransformPoint(bounds.center);
    }
}