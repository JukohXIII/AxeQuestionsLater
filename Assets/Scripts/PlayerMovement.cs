using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Vector2 moveInput;
    private Rigidbody2D rb;

    [Header("Ground movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 40f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private Transform visual;

    [Header("Dash")]
    [SerializeField] private float runBurstSpeed = 14f;
    [SerializeField] private float runBurstDuration = 0.2f;
    [SerializeField] private float runBurstAcceleration = 200f;

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

    private bool isGrounded;
    private PlayerInput playerInput;
    private InputAction jumpAction;
    private bool jumpReleased;
    private int airJumpsRemaining;
    private Collider2D playerCollider;
    private Collider2D lastPlatform;
    private Collider2D ignoredPlatform;
    private bool wasDownPressed;
    private float dropTimer;
    private bool isFastFalling;
    private float runBurstTimer;
    private float dashDirection;
    private float previousMoveX;
    private int facingDirection = 1;
    private float maxSpeed;
    private float accel;
    private float decel;
    private bool downJustPressed;
    

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        playerCollider = GetComponent<Collider2D>();
        jumpAction = playerInput.actions["Jump"];
    }

    void FixedUpdate()
    {
        checkGrounded();

        bool downPressed = moveInput.y < -0.5f;
        downJustPressed = downPressed && !wasDownPressed;

        bool newDirection = moveInput.x != 0 && (previousMoveX == 0 || Mathf.Sign(moveInput.x) != Mathf.Sign(previousMoveX));
        bool newFacingDirection = moveInput.x != 0 && Mathf.Sign(moveInput.x) != facingDirection;

        if (isGrounded)
        {
            airJumpsRemaining = maxAirJumps;
            isFastFalling = false;

            if (newDirection)
            {
                dashDirection = Mathf.Sign(moveInput.x);
                runBurstTimer = runBurstDuration;
            }

            maxSpeed = runBurstTimer > 0 ? runBurstSpeed : moveSpeed;
            accel = runBurstTimer > 0 ? runBurstAcceleration : acceleration;
            decel = deceleration;
        }
        else
        {
            maxSpeed = airSpeed;
            accel = airAcceleration;
            decel = airDeceleration;
            runBurstTimer = 0;
        }
        
        HandleHorizontalMovement();

        HandleDash();

        HandleJump();

        HandleFall();

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
    }

    void Update()
    {
        if (jumpAction.WasReleasedThisFrame())
        {
            jumpReleased = true;
        }
    }

    void checkGrounded()
    {
        Collider2D groundHit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayers);
        isGrounded = (rb.linearVelocity.y <= 0.1f) && (groundHit != null) && (groundHit.bounds.max.y - groundCheck.position.y <= groundedTolerance);
        lastPlatform = (isGrounded && groundHit.GetComponent<PlatformEffector2D>() != null) ? groundHit : null;
    }

    void HandleDash()
    {
        if (runBurstTimer > 0)
        {
            runBurstTimer -= Time.fixedDeltaTime;
            if (moveInput.x == 0 || Mathf.Sign(moveInput.x) != dashDirection)
            {
                runBurstTimer = 0;
            }
        }
    }

    void HandleHorizontalMovement()
    {
        if (moveInput.x == 0)
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
        
        if (!isGrounded && downJustPressed && rb.linearVelocity.y < 0)
        {
            isFastFalling = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -fastFallSpeed);
        }
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        // Debug.Log(moveInput);
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
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
            }
        }
        else if (value.isPressed && !isGrounded && airJumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            airJumpsRemaining--;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
