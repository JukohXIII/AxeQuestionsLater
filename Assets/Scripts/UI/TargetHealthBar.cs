using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// One health bar for the enemy the player is fighting: the last one hit, until it dies.
// If several enemies are hit on the same frame, the one closest to the player is picked.
public class TargetHealthBar : MonoBehaviour
{
    private const float DeathLinger = 0.4f;     // the emptied bar stays visible a moment after the kill
    private const float TrailDelay = 0.3f;      // the white trail waits before draining
    private const float TrailSpeed = 0.6f;      // fill ratio per second
    private const float OffsetAboveEnemy = 0.4f;   // world units above the collider

    [SerializeField] private GameObject root;   // visual root, toggled (this script must stay active)
    [SerializeField] private Image fill;
    [SerializeField] private Image trail;       // optional

    private readonly List<(Health health, float damage)> hitsThisFrame = new();
    private Health target;
    private Collider2D targetCollider;
    private Transform player;
    private Camera cam;
    private float trailRatio;
    private float trailHold;
    private float lingerTimer;
    private bool dying;

    void OnEnable()
    {
        Health.OnAnyDamaged += OnAnyDamaged;
        Hide();
    }

    void OnDisable() => Health.OnAnyDamaged -= OnAnyDamaged;

    void OnAnyDamaged(Health health, float damage)
    {
        if (health.TryGetComponent<EnemyController>(out _))
            hitsThisFrame.Add((health, damage));
    }

    void LateUpdate()
    {
        if (hitsThisFrame.Count > 0)
        {
            PickTarget();
            hitsThisFrame.Clear();
        }

        if (target == null) { Hide(); return; }   // destroyed

        float ratio = Ratio(target);
        fill.fillAmount = ratio;

        if (trail != null)
        {
            trailHold -= Time.deltaTime;
            if (trailHold <= 0f) trailRatio = Mathf.MoveTowards(trailRatio, ratio, TrailSpeed * Time.deltaTime);
            trail.fillAmount = Mathf.Max(trailRatio, ratio);
        }

        if (target.IsDead)
        {
            if (!dying) { dying = true; lingerTimer = DeathLinger; }
            lingerTimer -= Time.deltaTime;
            if (lingerTimer <= 0f) { Hide(); return; }
        }

        if (cam == null) cam = Camera.main;
        Bounds bounds = targetCollider.bounds;
        Vector3 above = new Vector3(bounds.center.x, bounds.max.y + OffsetAboveEnemy, 0f);
        Vector3 screen = cam.WorldToScreenPoint(above);
        root.transform.position = new Vector3(Mathf.Round(screen.x), Mathf.Round(screen.y), 0f);
    }

    void PickTarget()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        Health best = null;
        float bestDamage = 0f;
        float bestDistance = float.MaxValue;
        foreach (var (health, damage) in hitsThisFrame)
        {
            if (health == null) continue;
            float distance = player != null
                ? ((Vector2)health.transform.position - (Vector2)player.position).sqrMagnitude
                : 0f;
            if (distance < bestDistance) { bestDistance = distance; best = health; bestDamage = damage; }
        }
        if (best == null) return;

        if (best != target)
        {
            target = best;
            targetCollider = best.GetComponent<Collider2D>();
            trailRatio = Ratio(best);
        }

        // The trail starts from the ratio before this hit (or stays higher if it was still draining)
        trailRatio = Mathf.Min(1f, Mathf.Max(trailRatio, Ratio(best) + bestDamage / best.MaxHealth));
        trailHold = TrailDelay;
        dying = false;
        root.SetActive(true);
    }

    static float Ratio(Health h) => Mathf.Clamp01(h.CurrentHealth / h.MaxHealth);

    void Hide()
    {
        target = null;
        dying = false;
        if (root != null) root.SetActive(false);
    }
}