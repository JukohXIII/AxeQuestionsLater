using UnityEngine;

// Soul dropped by a dead enemy: hops out, falls to the ground, then gets pulled to the player when close.
public class SoulPickup : MonoBehaviour
{
    private const float BurstUpSpeed = 5f;
    private const float BurstSideSpeed = 2f;
    private const float Gravity = 25f;
    private const float AirDrag = 1f;            // horizontal slowdown while falling
    private const float GroundFriction = 8f;     // horizontal slowdown once landed
    private const float GroundOffset = 0f;     // distance between the orb center and the floor

    private const float CollectDelay = 0.5f;
    private const float MagnetRadius = 3f;
    private const float CollectRadius = 0.4f;
    private const float MagnetStartSpeed = 3f;
    private const float MagnetAcceleration = 45f;
    private const float MagnetMaxSpeed = 25f;

    private Collider2D target;
    private int value;
    private Vector2 velocity;
    private bool landed;
    private float age;
    private bool magnetized;
    private float magnetSpeed;
    private int groundMask;

    void Awake() => groundMask = LayerMask.GetMask("Ground", "Platform");

    public void Init(Collider2D player, int soulValue)
    {
        target = player;
        value = soulValue;
        velocity = new Vector2(
            Random.Range(-BurstSideSpeed, BurstSideSpeed),
            BurstUpSpeed * Random.Range(0.6f, 1f));
    }

    void Update()
    {
        if (target == null) return;

        float dt = Time.deltaTime;
        age += dt;

        Vector2 pos = transform.position;
        Vector2 toPlayer = (Vector2)target.bounds.center - pos;
        float dist = toPlayer.magnitude;

        if (age >= CollectDelay)
        {
            if (!magnetized && dist <= MagnetRadius)
            {
                magnetized = true;
                magnetSpeed = MagnetStartSpeed;
            }

            // The collect radius grows with the speed: a fast orb can't step over the player in one frame
            if (magnetized && dist <= Mathf.Max(CollectRadius, magnetSpeed * dt))
            {
                Collect();
                return;
            }
        }

        if (magnetized)
        {
            // Steering straight at the player (not a force): no orbiting, and the pull ramps up
            magnetSpeed = Mathf.Min(magnetSpeed + MagnetAcceleration * dt, MagnetMaxSpeed);
            transform.position = pos + toPlayer / dist * magnetSpeed * dt;
            return;
        }

        if (!landed) velocity.y -= Gravity * dt;
        velocity.x = Mathf.Lerp(velocity.x, 0f, (landed ? GroundFriction : AirDrag) * dt);

        Vector2 step = velocity * dt;

        // Only while falling: the orb can rise through one-way platforms but lands on them when coming down
        if (!landed && velocity.y <= 0f)
        {
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, -step.y + GroundOffset, groundMask);
            if (hit.collider != null)
            {
                pos.y = hit.point.y + GroundOffset;
                step.y = 0f;
                velocity.y = 0f;
                landed = true;
            }
        }

        transform.position = pos + step;
    }

    void Collect()
    {
        GameProgress.AddSouls(value);
        AudioManager.Play(Sfx.Soul);
        Destroy(gameObject);
    }
}