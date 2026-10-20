using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyConfig config;
    [Tooltip("If empty, found by the 'Player' tag at start.")]
    [SerializeField] private Transform player;
    [Tooltip("Ground and walls. The enemy itself must NOT be on this layer.")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Layer of the player. Used to detect who the attacks hit.")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Debug")]
    [SerializeField] private string currentStateName;
    public EnemyConfig Config => config;
    public Transform Player => player;
    public Rigidbody2D Rb { get; private set; }
    public Health Health { get; private set; }
    public int FacingDirection { get; private set; } = -1; // 1 for right, -1 for left

    // Attacks
    public EnemyAttackData CurrentAttack { get; private set; }
    private readonly Dictionary<EnemyAttackData, float> lastAttackTime = new Dictionary<EnemyAttackData, float>();
   
    // States
    public EnemyDecider Decider { get; private set;}
    public IEnemyState Idle { get; private set; }
    public IEnemyState Chase { get; private set; }
    public IEnemyState Attack { get; private set; }
    public IEnemyState Recovery { get; private set; }
    public IEnemyState Hitstun { get; private set; }
    public IEnemyState Dead { get; private set; }
    public IEnemyState Knockdown { get; private set; }
    public IEnemyState Retreat { get; private set; }
    public PlayerCombat PlayerCombat { get; private set; }
    public PlayerMovement PlayerMovement { get; private set; }
    private IEnemyState currentState;
    public IEnemyState CurrentState => currentState;
    private new BoxCollider2D collider;
    private float lastJumpTime = -999f;
    private Grabbable grabbable;

    // ----------- Lifecycle ----------- //
    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Health = GetComponent<Health>();
        collider = GetComponent<BoxCollider2D>();
        grabbable = GetComponent<Grabbable>();

        // Virtual factories: a subclass can swap any single state.
        Idle = CreateIdle();
        Chase = CreateChase();
        Attack = CreateAttack();
        Recovery = CreateRecovery();
        Hitstun = CreateHitstun();
        Dead = CreateDead();
        Knockdown = CreateKnockdown();
        Retreat = CreateRetreat();

        Decider = new EnemyDecider(this);

        // Ensure the enemy is facing the player at start
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * FacingDirection;
        transform.localScale = scale;
    }

    void OnEnable(){
        Health.OnHit += HandleHit;
        Health.OnDeath += HandleDeath;
    }

    void OnDisable(){
        Health.OnHit -= HandleHit;
        Health.OnDeath -= HandleDeath;
    }

    void Start()
    {
        if (player == null)
        {
            var playerGameObj = GameObject.FindGameObjectWithTag("Player");
            if (playerGameObj != null) player = playerGameObj.transform;
        }

        if (player != null)
        {
            PlayerCombat = player.GetComponent<PlayerCombat>();
            PlayerMovement = player.GetComponent<PlayerMovement>();
        }
        ChangeState(Idle);
    }

    void Update()
    {
        if (player == null) return; // No player to chase or attack
        if (grabbable != null && grabbable.IsGrabbed) return; // held: enemy freeze
        currentState?.Tick();

        if (currentState != null)
            currentStateName = currentState.GetType().Name
                + (Decider.LastDecision != null ? " [" + Decider.LastDecision.name + "]": "");

    }

    // ---------- State machine ----------
    public void ChangeState(IEnemyState next)
    {
        currentState?.Exit();
        currentState = next;
        currentStateName = currentState?.GetType().Name;
        currentState?.Enter();
    }

    public void GoTo(IEnemyState state)
    {
        if (currentState != state) ChangeState(state);
    }

    private void HandleHit() => ChangeState(Hitstun);
    private void HandleDeath() => ChangeState(Dead);
    protected virtual IEnemyState CreateIdle() => new IdleState(this);
    protected virtual IEnemyState CreateChase() => new ChaseState(this);
    protected virtual IEnemyState CreateAttack() => new AttackState(this);
    protected virtual IEnemyState CreateRecovery() => new RecoveryState(this);
    protected virtual IEnemyState CreateHitstun() => new HitstunState(this);
    protected virtual IEnemyState CreateDead() => new DeadState(this);
    protected virtual IEnemyState CreateKnockdown() => new KnockdownState(this);
    protected virtual IEnemyState CreateRetreat() => new RetreatState(this);

    // ---------- Perception ----------
    public float DistanceToPlayer() => Vector2.Distance(transform.position, player.position);
    public int DirectionToPlayer() => player.position.x >= transform.position.x ? 1 : -1;
    public float VerticalOffsetToPlayer() => player.position.y - transform.position.y;
    public bool IsPlayerInAttackRange() => Mathf.Abs(player.position.x - transform.position.x) 
        <= config.attackRange && Mathf.Abs(VerticalOffsetToPlayer()) <= config.verticalAttackTolerance;

    public bool IsPlayerAttackIncoming =>
        PlayerCombat != null
        && PlayerCombat.IsInAttackStartupOrActive
        && DistanceToPlayer() <= config.threatDistance;

    public bool IsPlayerRecovering =>
        PlayerCombat != null && PlayerCombat.IsInAttackRecovery;
        
    public bool IsGrounded()
    {
        Bounds bounds = collider.bounds;
        Vector2 size = new Vector2(bounds.size.x * 0.9f, 0.05f);
        return Physics2D.BoxCast(bounds.center, size, 0f, Vector2.down, 
            bounds.extents.y + 0.05f, groundLayer);
    }

    public bool IsObstacleAhead()
    {
        Bounds bounds = collider.bounds;
        return Physics2D.Raycast(bounds.center, Vector2.right * FacingDirection,
            bounds.extents.x + config.obstacleDetectionDistance, groundLayer);
    }

    // ---------- Attacks ----------
    public bool IsAttackReady(EnemyAttackData attackData) =>
        !lastAttackTime.TryGetValue(attackData, out float last) || Time.time >= last + attackData.cooldown;
    
    public bool IsPlayerInRangeOf(EnemyAttackData attackData)
    {
        float dx = Mathf.Abs(player.position.x - transform.position.x);
        return dx >= attackData.minRange && dx <= attackData.maxRange && 
            Mathf.Abs(VerticalOffsetToPlayer()) <= config.verticalAttackTolerance;
    }

    public bool CanUse(EnemyAttackData attack) =>
        IsAttackReady(attack)
        && IsPlayerInRangeOf(attack)
        && (!attack.requiresGrounded || IsGrounded());
       
    public void StartAttack(EnemyAttackData attack)
    {
        CurrentAttack = attack;
        lastAttackTime[attack] = Time.time;
        ChangeState(Attack);
    }

    public bool DealHit(EnemyHitData hit, HashSet<Health> alreadyHit)
    {
        bool hitSomeone = false;
        Vector2 center = (Vector2)transform.position
            + new Vector2(hit.hitboxOffset.x * FacingDirection, hit.hitboxOffset.y);

        Vector2 launch = hit.launchDirection == Vector2.zero ? Vector2.right : hit.launchDirection;
        Vector2 direction = new Vector2(launch.x * FacingDirection, launch.y).normalized;    
    
        foreach (Collider2D collider in Physics2D.OverlapBoxAll(center, hit.hitboxSize, 0f, playerLayer))
        {
            Health target = collider.GetComponentInParent<Health>();
            if (target == null || target == Health || alreadyHit.Contains(target)) continue;

            alreadyHit.Add(target);
            if (target.IsInvincible) continue;

            target.TakeHit(hit.damage, direction, hit.knockback, hit.causesKnockback, hit.breaksGuard);
            hitSomeone = true;
        }
        return hitSomeone;
    }

    // ---------- Actions ----------
    public void MoveHorizontal(float velocityX)
    {
        if (velocityX != 0f) Face(velocityX > 0f ? 1 : -1);
        Rb.linearVelocity = new Vector2(velocityX, Rb.linearVelocity.y);
    }

    public void StopHorizontal()
    {
        Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);
    }

    public float MaxJumpHeight()
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y) * Rb.gravityScale;
        return config.jumpVelocity * config.jumpVelocity / (2f*gravity);
    }

    public bool CanJump() => 
        IsGrounded() && Time.time >= lastJumpTime+config.jumpCooldown;
    
    public void Jump()
    {
        lastJumpTime = Time.time;
        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, config.jumpVelocity);
    }

    public void Face(int dir)
    {
        if (dir == FacingDirection) return;
        FacingDirection = dir;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;
    }

    // ---------- Debug ----------

    void OnDrawGizmosSelected()
    {
        if (config == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position,
            new Vector3(config.attackRange * 2f, config.verticalAttackTolerance * 2f, 0f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, config.threatDistance);

        if (config.decisionOptions == null) return;
        foreach (var option in config.decisionOptions)
            if (option is AttackOption attackOption && attackOption.attack != null)
                DrawHitGizmo(attackOption.attack.hit, Color.magenta);
    }

    void DrawHitGizmo(EnemyHitData hit, Color color)
    {
        Gizmos.color = color;
        Vector2 center = (Vector2)transform.position
            + new Vector2(hit.hitboxOffset.x * FacingDirection, hit.hitboxOffset.y);
        Gizmos.DrawWireCube(center, hit.hitboxSize);
    }
}