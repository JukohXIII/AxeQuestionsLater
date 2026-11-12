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
    private Vector2 lastVelocity;

    [Header("Wall")]
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private float wallBounceSpeed = 8f;
    [SerializeField] private float wallPopUp = 6f;
    [SerializeField] private float minWallImpactSpeed = 4f;
    private EnemyController enemy;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        enemy = GetComponent<EnemyController>();
    }

    void FixedUpdate() { lastVelocity = rb.linearVelocity; }

    void OnEnable() { health.OnHit += StartKnockbackWindow; }
    void OnDisable() { health.OnHit -= StartKnockbackWindow; }

    void StartKnockbackWindow() { knockbackWindow = knockbackWindowDuration; }

    void Update()
    {
        if (knockbackWindow > 0) knockbackWindow -= Time.deltaTime;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.enabled) return;
        if (knockbackWindow <= 0) return;
        if (health.IsDead) return;

        Vector2 normal = collision.GetContact(0).normal;
        float impactSpeed = Mathf.Abs(Vector2.Dot(lastVelocity, normal));
        if (((1 << collision.gameObject.layer) & wallLayers) != 0)
        {
            if (impactSpeed > minWallImpactSpeed && enemy.CurrentState == enemy.Hitstun)
            {
                if (Mathf.Abs(normal.x) > 0.5f)
                {
                    rb.linearVelocity = new Vector2(normal.x * wallBounceSpeed, wallPopUp);
                }
                else
                {
                    rb.linearVelocity = new Vector2(lastVelocity.x, normal.y * wallBounceSpeed);
                }
                enemy.ChangeState(enemy.Hitstun);
            }
            return;
        }

        if (((1 << collision.gameObject.layer) & groundLayers) == 0) return;

        if (impactSpeed > minBounceSpeed && enemy.CurrentState == enemy.Hitstun)
            rb.linearVelocity = Vector2.Reflect(lastVelocity, normal) * bounceDamping;
    }
}