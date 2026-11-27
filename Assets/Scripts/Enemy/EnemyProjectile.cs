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
    private const float ExplosionDuration = 0.43f; // length of Snowball_explosion
    [SerializeField] private Animator animator;
    public const float LobGravity = 25f;
    private const float LobAngle = 50 * Mathf.Deg2Rad;
    private const float MinLobDx = 0.5f;
    private Vector2 velocity;
    private float gravity;
    void Awake() => rb = GetComponent<Rigidbody2D>();
    
    public void Launch(Vector2 direction, float speed, EnemyHitData hitData, LayerMask targets, LayerMask ground)
    {
        Setup(hitData, targets, ground);
        velocity = direction * speed;
        rb.linearVelocity = velocity;
    }

    public void LaunchLob(Vector2 launchVelocity, EnemyHitData hitData, LayerMask targets, LayerMask ground)
    {
        Setup(hitData, targets, ground);
        gravity = LobGravity;
        velocity = launchVelocity;
        rb.linearVelocity = velocity;
    }

    void Setup(EnemyHitData hitData, LayerMask targets, LayerMask ground)
    {
        hit = hitData;
        targetLayer = targets;
        groundLayer = ground;
        Destroy(gameObject, Lifetime);
    }

    void FixedUpdate()
    {
        if (gravity == 0f || consumed) return;
        velocity.y -= gravity * Time.fixedDeltaTime;
        rb.linearVelocity = velocity;
    }

    public static bool TryAimLob(Vector2 delta, out Vector2 launchVelocity)
    {
        launchVelocity = Vector2.zero;
        float dx = Mathf.Abs(delta.x);
        if (dx < MinLobDx) return false;

        float cos = Mathf.Cos(LobAngle);
        float denominator = 2f * cos * cos * (dx * Mathf.Tan(LobAngle) - delta.y);
        if (denominator <= 0f) return false;

        float speed = Mathf.Sqrt(LobGravity * dx * dx / denominator);
        launchVelocity = new Vector2(Mathf.Sign(delta.x) * speed * cos, speed * Mathf.Sin(LobAngle));
        return true;
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
            if (other.isTrigger)
            {
                // Trigger on the target layer = the attack hitbox, only enabled during the active frames of a swing
                if (other.GetComponentInParent<PlayerCombat>() != null) Despawn();
                return;
            }
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
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // no more hits while the explosion plays
        animator.Play("Snowball_explosion", 0, 0f);
        Destroy(gameObject, ExplosionDuration); // shorter delay than the Lifetime one: the earliest wins
    }
}