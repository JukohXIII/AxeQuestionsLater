using UnityEngine;

public class EnemyKnockbackBounce : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float bounceDamping = 0.4f;
    [SerializeField] private float minBounceSpeed = 5f;
    [SerializeField] private float knockbackWindowDuration = 3f;
    private float knockbackWindow;
    private Health health;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
    }

    void OnEnable() { health.OnHit += StartKnockbackWindow; }
    void OnDisable() { health.OnHit -= StartKnockbackWindow; }

    void StartKnockbackWindow() { knockbackWindow = knockbackWindowDuration; }

    void Update()
    {
        if (knockbackWindow > 0) knockbackWindow -= Time.deltaTime;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (knockbackWindow <= 0) return;
        if (((1 << collision.gameObject.layer) & groundLayers) != 0)
        {
            float impactSpeed = collision.relativeVelocity.y;
            if (impactSpeed > minBounceSpeed)
            {
                float newY = impactSpeed * bounceDamping;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
            }
        }
    }
}