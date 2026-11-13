using System.Collections.Generic;
using UnityEngine;

public class ThrownCollateral : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float knockback = 8f;
    [SerializeField] private float landGrace = 0.1f;
    private float elapsed;
    private bool active;
    [SerializeField] private Vector2 hitboxSize = new (1f, 1f);
    [SerializeField] private float minSpeed = 2f;

    private Collider2D self;
    private readonly HashSet<Collider2D> alreadyHit = new ();
    private Rigidbody2D rb;

    void Awake()
    {
        self = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch()
    {
        active = true;
        elapsed = 0f;
    }

    void FixedUpdate()
    {
        if (!active) return;
        elapsed += Time.fixedDeltaTime;

        bool afterGrace = elapsed > landGrace;
        if (elapsed > landGrace && rb.linearVelocity.magnitude < minSpeed)
        {
            active = false;
            return;
        }

        foreach (Collider2D hit in Physics2D.OverlapBoxAll(transform.position, hitboxSize, 0f, enemyLayers))
        {
            if (hit == self) continue;
            if (!hit.TryGetComponent(out Health h) || h.IsDead) continue;

            alreadyHit.Add(hit);
            float side = Mathf.Sign(hit.transform.position.x - transform.position.x);
            h.TakeHit(damage, new Vector2(side, 0.5f).normalized, knockback, false);
            active = false;
            return;
        }
    }
}