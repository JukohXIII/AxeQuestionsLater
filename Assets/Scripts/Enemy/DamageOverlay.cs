using UnityEngine;

[RequireComponent(typeof(Health))]
public class DamageOverlay : MonoBehaviour
{
    [Tooltip("The sprite to cover. If empty, the first SpriteRenderer in the children.")]
    [SerializeField] private SpriteRenderer target;
    [Tooltip("Optional unlit material, so the overlay ignores 2D lights. If empty, uses the sprite's material.")]
    [SerializeField] private Material overlayMaterial;

    [Header("Damage tint")]
    [SerializeField] private Color overlayColor = Color.red;
    [Tooltip("Overlay opacity when the enemy is almost dead. Below 1 so the sprite stays readable.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.6f;
    [Tooltip("How fast the red builds up, in alpha per second.")]
    [SerializeField] private float fadeSpeed = 3f;

    [Header("Hit flash")]
    [Tooltip("The alpha of this color is the flash's peak intensity.")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    private Health health;
    private SpriteRenderer overlay;
    private float currentAlpha;
    private float flashTimer;
    void Awake()
    {
        health = GetComponent<Health>();
        if (target == null) target = GetComponentInChildren<SpriteRenderer>();

        // Second renderer, child of the sprite, drawn right above it
        var gameObject = new GameObject("Damage Overlay");
        gameObject.transform.SetParent(target.transform, false);
        overlay = gameObject.AddComponent<SpriteRenderer>();
        overlay.sharedMaterial = overlayMaterial != null ? overlayMaterial : target.sharedMaterial;
        overlay.sortingLayerID = target.sortingLayerID;
        overlay.sortingOrder = target.sortingOrder + 1;
        overlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
    }

    void OnEnable()
    {
        health.OnHit += StartFlash;
        health.OnDeath += StartFlash; // OnHit isn't raised on the killing blow
    }

    void OnDisable()
    {
        health.OnHit -= StartFlash;
        health.OnDeath -= StartFlash;
    }

    void StartFlash() => flashTimer = flashDuration;

    void LateUpdate()
    {
        // Mirror whatever frame the animation is showing
        overlay.sprite = target.sprite;
        overlay.flipX = target.flipX;
        overlay.flipY = target.flipY;
        overlay.enabled = target.enabled;

        // Red: a state, read every frame
        float targetAlpha = health.IsDead ? 0f : health.DamageRatio * maxAlpha ;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

        // Flash: a moment, triggered by the events above, decays from 1 to 0
        flashTimer = Mathf.Max(0f, flashTimer - Time.deltaTime);
        float flash = flashDuration > 0f ? flashTimer / flashDuration : 0f;

        Color tint = overlayColor;
        tint.a = currentAlpha;
        overlay.color = Color.Lerp(tint, flashColor, flash);
    }
}