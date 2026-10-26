using TMPro;
using UnityEngine;

// Permanent soul counter for the HUD. Updated through GameProgress.SoulsChanged,
// so it reacts to both gains (SoulCollector) and spending (UpgradeRow -> TrySpend).
public class SoulCounter : MonoBehaviour
{
    private const string Format = "{0} souls";       // e.g. "Souls: {0}" to add a text prefix
    private const float PunchScale = 1.25f;    // how much the text swells when the value changes
    private const float PunchDuration = 0.2f;

    [SerializeField] private TMP_Text label;

    private Vector3 baseScale;
    private float punchTimer;

    void Awake() => baseScale = label.transform.localScale;

    void OnEnable()
    {
        GameProgress.SoulsChanged += OnSoulsChanged;

        // Initial display without animation (scene change, back to the hub)
        label.text = string.Format(Format, GameProgress.Souls);
        label.transform.localScale = baseScale;
        punchTimer = 0f;
    }

    void OnDisable() => GameProgress.SoulsChanged -= OnSoulsChanged;

    void OnSoulsChanged()
    {
        label.text = string.Format(Format, GameProgress.Souls);
        punchTimer = PunchDuration;
    }

    void Update()
    {
        if (punchTimer <= 0f) return;

        punchTimer = Mathf.Max(0f, punchTimer - Time.unscaledDeltaTime);

        // 0 -> 1 -> 0 over the duration: the text swells, then returns to its size
        float t = 1f - punchTimer / PunchDuration;
        float k = Mathf.Sin(t * Mathf.PI);
        label.transform.localScale = baseScale * Mathf.Lerp(1f, PunchScale, k);
    }
}