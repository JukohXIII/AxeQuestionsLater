using UnityEngine;
using System.Collections;

public class HitVisualCue : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    private Health health;
    private Color originalColor;

    void Awake()
    {
        health = GetComponent<Health>();
        originalColor = sprite.color;
    }

    void OnEnable() { health.OnHit += Flash; }
    void OnDisable() { health.OnHit -= Flash; }

    void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        sprite.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        sprite.color = originalColor;
    }
}