using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    private const float Lifetime = 5f;

    private Rigidbody2D rb;
    private EnemyHitData hit;
    private LayerMask targetLayer;
    private LayerMask groundLayer;
    private bool consumed;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Launch(Vector2 direction, float speed, EnemyHitData hitData, LayerMask targets, LayerMask ground)
    {
        hit = hitData;
        targetLayer = targets;
        groundLayer = ground;
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, Lifetime);
    }

    void OnTriggerEnter2D(Collider2D other) => Resolve(other);
    // Stay: a target whose i-frames end while overlapping the projectile still gets hit
    void OnTriggerStay2D(Collider2D other) => Resolve(other);

    void Resolve(Collider2D other)
    {
        if (consumed || hit == null) return;

        int bit = 1 << other.gameObject.layer;

        if ((targetLayer.value & bit) != 0)
        {
            Health target = other.GetComponentInParent<Health>();
            if (target == null || target.IsInvincible) return;   // dash i-frames: the projectile flies through

            Vector2 launch = hit.launchDirection == Vector2.zero ? Vector2.right : hit.launchDirection;
            float side = Mathf.Sign(rb.linearVelocity.x);   // X of the launch is mirrored by the flight direction
            Vector2 direction = new Vector2(launch.x * side, launch.y).normalized;

            target.TakeHit(hit.damage, direction, hit.knockback, hit.causesKnockback, hit.breaksGuard);
            Despawn();
        }
        else if ((groundLayer.value & bit) != 0 && !other.usedByEffector)   // one-way platforms are not walls
        {
            Despawn();
        }
    }

    void Despawn()
    {
        consumed = true;
        // TODO: impact VFX
        Destroy(gameObject);
    }
}