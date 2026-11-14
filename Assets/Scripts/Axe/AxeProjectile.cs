using System;
using UnityEngine;

public class AxeProjectile : MonoBehaviour
{
    private enum AxeState { Flying, Stuck, Returning }

    [Header("Throw")]
    [SerializeField] private float throwSpeed = 18f;
    [SerializeField] private float gravity = 20f;      // pulls the axe down into an arc
    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockback = 6f;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 25f;
    [SerializeField] private float catchDistance = 0.5f;

    [Header("Detection")]
    [SerializeField] private float radius = 0.3f;
    [SerializeField] private LayerMask enemyLayers;     // Enemy
    [SerializeField] private LayerMask stickLayers;     // Ground, Platform, Wall
    [SerializeField] private LayerMask wallLayers;      // Wall
    [SerializeField] private float wallBounce = 0.3f;   // how much speed it keeps after the bounce
    private bool bounced;

    public event Action OnCaught;   // Lars listens to get the axe back

    private AxeState state;
    private Vector2 velocity;
    private bool hasHitEnemy;       // only the first enemy gets hit
    private Transform owner;
    private Transform stuckTo;      // enemy we're stuck in (null = ground/wall)
    private Health stuckHealth;
    private Vector3 stuckOffset;
    private Animator animator;

    void Awake() { animator = GetComponent<Animator>(); }

    public void Launch(Transform thrower, int facing)
    {
        owner = thrower;
        velocity = new Vector2(facing * throwSpeed, 0f);
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facing;   // -0.15 when thrown left
        transform.localScale = scale;
        state = AxeState.Flying;
    }

    void Update()
    {
        if (state == AxeState.Flying) Fly();
        else if (state == AxeState.Stuck) FollowTarget();
        else if (state == AxeState.Returning) ReturnToOwner();
    }

    void Fly()
    {
        velocity.y -= gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        if (IsOffScreen()) { StartReturn(); return; }

        if (!hasHitEnemy)
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, radius, enemyLayers);
            if (hit != null && hit.TryGetComponent(out Health h) && !h.IsDead)
            {
                hasHitEnemy = true;
                h.TakeHit(damage, new Vector2(Mathf.Sign(velocity.x), 0.3f).normalized, knockback);
                StickTo(hit.transform, h);
                return;
            }
        }

        if (!bounced && Physics2D.OverlapCircle(transform.position, radius, wallLayers))
        {
            bounced = true;
            hasHitEnemy = true;  // a bounced axe is harmless
            velocity = new Vector2(-velocity.x * wallBounce, 0f);
            return;
        }

        if (Physics2D.OverlapCircle(transform.position, radius, stickLayers))
            StickTo(null, null);
    }

    void StickTo(Transform target, Health h)
    {
        state = AxeState.Stuck;
        stuckTo = target;
        stuckHealth = h;
        if (target != null) stuckOffset = transform.position - target.position;
        animator.enabled = false;   // stop spinning
    }

    public void Recall()
    {
        if (state != AxeState.Stuck) return;   // player can only recall a stuck axe
        StartReturn();
    }

    void StartReturn()
    {
        state = AxeState.Returning;
        stuckTo = null;
        animator.enabled = true;   // spin again on the way back
    }

    void FollowTarget()
    {
        if (stuckTo == null) return;   // stuck in ground/wall: stay put

        if (stuckHealth.IsDead)        // enemy died: the axe drops
        {
            stuckTo = null;
            velocity = Vector2.zero;
            state = AxeState.Flying;   // falls until it hits the ground
            return;
        }
        transform.position = stuckTo.position + stuckOffset;
    }

    void ReturnToOwner()
    {
        transform.position = Vector2.MoveTowards(transform.position, owner.position, returnSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, owner.position) <= catchDistance)
        {
            OnCaught?.Invoke();
            Destroy(gameObject);
        }
    }

    bool IsOffScreen()
    {
        Vector3 view = Camera.main.WorldToViewportPoint(transform.position);
        return view.x < -0.1f || view.x > 1.1f || view.y < -0.1f;
    }
}