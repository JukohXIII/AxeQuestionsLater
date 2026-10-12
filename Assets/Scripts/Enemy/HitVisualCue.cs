using UnityEngine;
using System.Collections;

public class HitVisualCue : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    private Health health;
    private Color originalColor;
    [SerializeField] private float blinkInterval = 0.08f;
    private float blinkTimer;
    private bool wasBlinking;

    void Awake()
    {
        health = GetComponent<Health>();
        originalColor = sprite.color;
    }

    void Update()
    {
        if (health.IsInvincible)
        {
            blinkTimer += Time.deltaTime;
            bool white = (int)(blinkTimer / blinkInterval) % 2 == 0;
            sprite.color = white ? flashColor : originalColor;
            wasBlinking = true;
        }
        else if (wasBlinking)
        {
            sprite.color = originalColor;
            blinkTimer = 0;
            wasBlinking = false;
        }
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