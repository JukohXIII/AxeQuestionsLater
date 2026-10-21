using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDefeat : MonoBehaviour
{
    [SerializeField] private float defeatPoseTime = 1.5f;   // time kneeling before running
    [SerializeField] private float runAwaySpeed = 5f;
    [SerializeField] private float runAwayTime = 1.5f;

    [Header("Dropped axe")]
    [SerializeField] private GameObject droppedAxePrefab;
    [SerializeField] private Vector2 axeDropOffset = new Vector2(1.3f, -0.2f);
    [SerializeField] private float axeDropTime = 1.22f;     // when the drawn axe hits the ground

    // ---------- References ----------
    private Health health;
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;
    private Rigidbody2D rb;

    // ---------- State (PlayerAnimation reads these) ----------
    public bool DefeatLanded { get; private set; }
    public bool IsRunningAway { get; private set; }
    private float landCheckDelay;
    private float defeatTimer;
    private float defeatClipTimer;
    private bool axeDropped;
    private bool gameOverShown;

    void Awake()
    {
        health = GetComponent<Health>();
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Wall"), false);
    }

    void OnEnable()  { health.OnDeath += HandleDeath; }
    void OnDisable() { health.OnDeath -= HandleDeath; }

    void HandleDeath()
    {
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);
        playerCombat.Interrupt(PlayerCombat.PlayerState.Defeated);
        GetComponent<PlayerInput>().DeactivateInput();   // no more control

        defeatTimer = defeatPoseTime + runAwayTime;
        landCheckDelay = 0.2f;
        DefeatLanded = false;
        defeatClipTimer = 0f;
        axeDropped = false;
    }

    void Update()
    {
        if (playerCombat.state != PlayerCombat.PlayerState.Defeated || gameOverShown) return;

        if (!DefeatLanded)
        {
            landCheckDelay -= Time.deltaTime;
            if (landCheckDelay > 0f || !playerMovement.IsGrounded) return;   // still flying
            DefeatLanded = true;
        }

        defeatClipTimer += Time.deltaTime;   // the defeat clip starts when he lands
        if (!axeDropped && defeatClipTimer >= axeDropTime) DropAxe();

        defeatTimer -= Time.deltaTime;
        if (defeatTimer <= runAwayTime && !IsRunningAway) StartRunningAway();
        if (IsRunningAway) rb.linearVelocity = new Vector2(-runAwaySpeed, rb.linearVelocity.y);

        if (IsRunningAway && (IsOffScreen() || defeatTimer <= -3f))   // -3: safety, in case he gets stuck
        {
            gameOverShown = true;
            FindAnyObjectByType<GameOverMenu>().Show();
        }
    }

    void StartRunningAway()
    {
        IsRunningAway = true;
        playerMovement.SetFacing(-1);   // walk_withoutAxe is drawn facing right, so flip to face left
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Wall"), true);
    }

    bool IsOffScreen()
    {
        Vector3 view = Camera.main.WorldToViewportPoint(transform.position);
        return view.x < -0.1f || view.x > 1.1f || view.y < -0.1f;
    }

    void DropAxe()
    {
        axeDropped = true;
        if (droppedAxePrefab == null) return;

        int facing = playerMovement.FacingDirection;
        Vector2 pos = (Vector2)transform.position + new Vector2(axeDropOffset.x * facing, axeDropOffset.y);
        GameObject axe = Instantiate(droppedAxePrefab, pos, Quaternion.identity);
        axe.GetComponent<SpriteRenderer>().flipX = facing < 0;   // mirror it when Lars faces left
    }
}