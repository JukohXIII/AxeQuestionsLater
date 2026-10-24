using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class FadeWhenPlayerInside : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.12f;
    [Tooltip("0 = disparaît complètement, 0.3 = reste légèrement visible.")]
    [SerializeField, Range(0f, 1f)] private float alphaWhenInside = 0f;
    [SerializeField] private string playerTag = "Player";

    private SpriteRenderer sprite;
    private float visibleAlpha;   // l'alpha d'origine du sprite
    private float targetAlpha;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        visibleAlpha = sprite.color.a;
        targetAlpha = visibleAlpha;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) targetAlpha = visibleAlpha * alphaWhenInside;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) targetAlpha = visibleAlpha;
    }

    void Update()
    {
        Color c = sprite.color;
        if (Mathf.Approximately(c.a, targetAlpha)) return;

        c.a = Mathf.MoveTowards(c.a, targetAlpha, Time.unscaledDeltaTime / Mathf.Max(fadeDuration, 0.01f));
        sprite.color = c;
    }
}