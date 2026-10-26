using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // ---------- References ----------
    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private PlayerInput playerInput;
    private InputAction jumpAction;
    private PlayerCombat playerCombat;

    [Header("Ground movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 40f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private Transform visual;

    [Header("Burst movement")]
    [SerializeField] private float burstMovementSpeed = 14f;
    [SerializeField] private float burstMovementDuration = 0.2f;
    [SerializeField] private float burstMovementAcceleration = 200f;

    [Header("Jump movement")]
    [SerializeField] private float jumpVelocity = 10f;
    [SerializeField] private float jumpCutMultiplier = 0.1f;
    [SerializeField] private int maxAirJumps = 1;
    [SerializeField] private float groundedTolerance = 0.1f;
    [SerializeField] private float dropTime = 0.3f;

    [Header("Fall movement")]
    [SerializeField] private float maxFallSpeed = 15f;
    [SerializeField] private float fastFallSpeed = 25f;

    [Header("Air movement")]
    [SerializeField] private float airSpeed = 8f;
    [SerializeField] private float airAcceleration = 30f;
    [SerializeField] private float airDeceleration = 5f;

    [Header("Dash movement")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.4f;
    [SerializeField] private float dashAttackWindow = 0.2f;

    [Header("Enemy bounce")]
    [SerializeField] private float enemyBounceVelocity = 12f;
    [SerializeField] private LayerMask enemyLayer;

    // ---------- Read by other scripts ----------
    public int FacingDirection => facingDirection;
    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool CanDashAttack => isDashing || dashAttackWindowTimer > 0;
    private bool CanJumpNow => !isDashing && playerCombat.state != PlayerCombat.PlayerState.Grabbing && !playerCombat.IsHurt && !playerGuard.IsInBlockstun;
    private PlayerGuard playerGuard;


    // ---------- Input ----------
    private Vector2 moveInput;
    private bool jumpReleased;
    private bool wasDownPressed;
    private bool downJustPressed;
    private float previousMoveX;

    // ---------- Ground, speed & facing ----------
    private bool isGrounded;
    private int facingDirection = 1;
    private float maxSpeed;
    private float accel;
    private float decel;
    private float runBurstTimer;
    private float burstMovementDirection;

    // ---------- Jump & fall ----------
    private int airJumpsRemaining;
    private bool isFastFalling;
    private Collider2D lastPlatform;
    private Collider2D ignoredPlatform;
    private float dropTimer;

    // ---------- Dash ----------
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private float dashAttackWindowTimer;
    private float defaultGravityScale;
    
    [Header("Audio")]
    [SerializeField] private float stepDistance = 10f;
    [SerializeField] private float minLandingSpeed = 4f;
    [SerializeField] private float minStepSpeed = 0.5f;
    [SerializeField] private float minStepInterval = 0.12f;  
    private bool wasWalking;
    private float lastStepTime = -1f;    private float stepAccumulator;
    private bool wasGroundedPrev;
    private float prevVelocityY;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        playerCollider = GetComponent<Collider2D>();
        jumpAction = playerInput.actions["Jump"];
        defaultGravityScale = rb.gravityScale;
        playerCombat = GetComponent<PlayerCombat>();
        playerGuard = GetComponent<PlayerGuard>();

        StatBases.Register(StatType.MoveSpeed, moveSpeed);
        StatBases.Register(StatType.AirJumps, maxAirJumps);
        StatBases.Register(StatType.DashCooldown, dashCooldown);
    }

    void FixedUpdate()
    {
        CheckGrounded();

        if (isGrounded && !wasGroundedPrev && -prevVelocityY > minLandingSpeed)
            AudioManager.Play(Sfx.Land);
        wasGroundedPrev = isGrounded;

        bool downPressed = moveInput.y < -0.5f;
        downJustPressed = downPressed && !wasDownPressed;

        bool newDirection = moveInput.x != 0 && (previousMoveX == 0 || Mathf.Sign(moveInput.x) != Mathf.Sign(previousMoveX));
        bool newFacingDirection = moveInput.x != 0 && Mathf.Sign(moveInput.x) != facingDirection && playerCombat.state == PlayerCombat.PlayerState.Normal;

        if (isGrounded)
        {
            airJumpsRemaining = Mathf.RoundToInt(PlayerStats.Apply(StatType.AirJumps, maxAirJumps));
            isFastFalling = false;

            if (newDirection)
            {
                burstMovementDirection = Mathf.Sign(moveInput.x);
                runBurstTimer = burstMovementDuration;
            }

            maxSpeed = PlayerStats.Apply(StatType.MoveSpeed, runBurstTimer > 0 ? burstMovementSpeed : moveSpeed);
            accel = runBurstTimer > 0 ? burstMovementAcceleration : acceleration;
            decel = deceleration;
        }
        else
        {
            maxSpeed = PlayerStats.Apply(StatType.MoveSpeed, airSpeed);
            accel = airAcceleration;
            decel = airDeceleration;
            runBurstTimer = 0;
        }
        
        HandleHorizontalMovement();

        HandleBurstMovement();

        HandleJump();

        HandleFall();

        HandleDash();

        HandleFootsteps();

        float limit = isFastFalling ? fastFallSpeed : maxFallSpeed;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -limit));

        if (newFacingDirection)
        {
            facingDirection = (int)Mathf.Sign(moveInput.x);
        }
        Vector3 s = visual.localScale;
        visual.localScale = new Vector3(Mathf.Abs(s.x) * facingDirection, s.y, s.z);

        wasDownPressed = downPressed;
        previousMoveX = moveInput.x;
        prevVelocityY = rb.linearVelocity.y;
    }

    void Update()
    {
        if (jumpAction.WasReleasedThisFrame())
        {
            jumpReleased = true;
        }
    }

    void CheckGrounded()
    {
        Collider2D groundHit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayers);
        isGrounded = (rb.linearVelocity.y <= 0.1f) && (groundHit != null) && (groundHit.bounds.max.y - groundCheck.position.y <= groundedTolerance);
        lastPlatform = (isGrounded && groundHit.GetComponent<PlatformEffector2D>() != null) ? groundHit : null;
    }

    void HandleBurstMovement()
    {
        if (runBurstTimer > 0)
        {
            runBurstTimer -= Time.fixedDeltaTime;
            if (moveInput.x == 0 || Mathf.Sign(moveInput.x) != burstMovementDirection)
            {
                runBurstTimer = 0;
            }
        }
    }

    void HandleHorizontalMovement()
    {
        if (playerCombat.state == PlayerCombat.PlayerState.Defeated) return;
        if (playerCombat.IsHurt && !isGrounded) return;
        bool lockedOnGround = isGrounded && playerCombat.state != PlayerCombat.PlayerState.Normal;

        if (moveInput.x == 0 || lockedOnGround  )
        {
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0, decel * Time.fixedDeltaTime), rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, moveInput.x * maxSpeed, accel * Time.fixedDeltaTime), rb.linearVelocity.y);
        }
    }

    void HandleJump()
    {
        if (jumpReleased)
        {
            jumpReleased = false;
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }
    }

    void HandleFall()
    {
        if (ignoredPlatform != null)
        {
            dropTimer -= Time.fixedDeltaTime;
            if (dropTimer <= 0)
            {
                Physics2D.IgnoreCollision(playerCollider, ignoredPlatform, false);
                ignoredPlatform = null;
            }
        }
        
        if (!isGrounded && downJustPressed && rb.linearVelocity.y < 0 && !playerCombat.IsHurt)
        {
            isFastFalling = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -fastFallSpeed);
        }
    }

    void PlayStep(float volumeScale = 1f)
    {
        lastStepTime = Time.time;
        AudioManager.Play(Sfx.Footstep, volumeScale);
    }

    void HandleFootsteps()
    {
        if (!isGrounded) { stepAccumulator = 0; wasWalking = false; return; };

        bool free = !isDashing && playerCombat.state == PlayerCombat.PlayerState.Normal;
        bool walking = free && moveInput.x != 0f && Mathf.Abs(rb.linearVelocity.x ) > minStepSpeed;
        
        if (!walking)
        {
            if (wasWalking && free && moveInput.x == 0f && Time.time - lastStepTime > minStepInterval)
                PlayStep();

                wasWalking = false;
                stepAccumulator = stepDistance;
                return;
        }

        wasWalking = true;
        stepAccumulator += Mathf.Abs(rb.linearVelocity.x) * Time.fixedDeltaTime;
        if(stepAccumulator >= stepDistance)
        {
            stepAccumulator = 0f;
            PlayStep();
        }
    }

    void HandleDash()
    {
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.fixedDeltaTime;
        }
        if (dashAttackWindowTimer > 0)
        {
            dashAttackWindowTimer -= Time.fixedDeltaTime;
        }
        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0)
            {
                EndDash();
                dashAttackWindowTimer = dashAttackWindow;
            }
            else
            {
                rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0);
            }
        }
    }

    public void EndDash()
    {
        isDashing = false;
        rb.gravityScale = defaultGravityScale;
        dashCooldownTimer = PlayerStats.Apply(StatType.DashCooldown, dashCooldown);
        dashAttackWindowTimer = 0;
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded && CanJumpNow)
        {
            if (moveInput.y < -0.5f && lastPlatform != null && ignoredPlatform == null)
            {
                Physics2D.IgnoreCollision(playerCollider, lastPlatform, true);
                ignoredPlatform = lastPlatform;
                dropTimer = dropTime;
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
                isGrounded = false;
                AudioManager.Play(Sfx.Jump);
            }
        }
        else if (value.isPressed && !isGrounded && airJumpsRemaining > 0 && CanJumpNow)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            airJumpsRemaining--;
            AudioManager.Play(Sfx.AirJump);
        }
    }

    void OnDash(InputValue value)
    {
        if (value.isPressed && isGrounded && !isDashing && dashCooldownTimer <= 0 && playerCombat.state == PlayerCombat.PlayerState.Normal)
        {
            isDashing = true;
            dashTimer = dashDuration;
            rb.gravityScale = 0f;
            AudioManager.Play(Sfx.AirJump);
        }
    }

    public void SetFacing(int direction)
    {
        facingDirection = direction;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (playerCombat.IsHurt) return;
        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, enemyBounceVelocity);
                    break;
                }
            }
        }
    }

    public void SetAirHang(bool hang)
    {
        if (hang)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, 0f);
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }
    }

    public void ClearInput()
    {
        moveInput = Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
