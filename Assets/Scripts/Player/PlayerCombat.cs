using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AttackData
{
    public string attackName;
    public string animationName;
    public float startup;
    public float active;
    public float recovery;
    public float damage;
    public float knockback;
    public Vector2 launchDirection;
    public Vector2 hitboxOffset = new Vector2(0.7f, 0f);
    public Vector2 hitboxSize = new Vector2(0.7f, 0.7f);
    public bool causesKnockdown;
}

[System.Serializable]
public class ThrowData
{
    public float damage;
    public float knockback;
    public Vector2 direction;
    public bool causesKnockdown;
    public string animationName;
    public float releaseDelay = 0.15f;
}

public class PlayerCombat : MonoBehaviour
{

    public enum PlayerState { Normal, Attacking, Grabbing, Blocking, Hurt, Knockdown, Defeated }
    private enum AttackPhase { None, Startup, Active, Recovery }

    // ---------- References ----------
    private PlayerMovement playerMovement;


    // ---------- State (other scripts read these) ----------
    [Header("State")]
    public PlayerState state = PlayerState.Normal;
    public string CurrentAnimation { get; private set; }
    public int AttackId { get; private set; }
    public bool IsHurt => state == PlayerState.Hurt || state == PlayerState.Knockdown || state == PlayerState.Defeated;

    // ---------- Input ----------
    private Vector2 moveInput;
    private bool attackPressedThisFrame;
    private bool grabPressedThisFrame;

    // ---------- Attack phases ----------
    [SerializeField] private float hitStopDuration = 0.05f;
    private AttackData currentAttack;
    private AttackPhase phase = AttackPhase.None;
    private float phaseTimer;
    private bool isComboAttack;
    private bool queuedNextHit;
    private int comboStep;
    [SerializeField] private float comboWindow = 0.3f;   // time after a jab to still continue the combo
    private float comboTimer;

    // For enemy AI: reading the player's attack phase
    public bool IsInAttackRecovery =>
        state == PlayerState.Attacking && phase == AttackPhase.Recovery;

    public bool IsInAttackStartupOrActive =>
        state == PlayerState.Attacking && (phase == AttackPhase.Startup || phase == AttackPhase.Active);

    [Header("Hitbox")]
    [SerializeField] private GameObject attackHitbox;
    [SerializeField] private LayerMask hittableLayers;
    private readonly HashSet<Collider2D> hitTargetsThisSwing = new HashSet<Collider2D>();

    [Header("Ground Attacks")]
    [SerializeField] private AttackData[] comboAttacks;
    [SerializeField] private AttackData upTiltData;
    [SerializeField] private AttackData downTiltData;
    [SerializeField] private AttackData dashAttackData;

    [Header("Air Attacks")]
    [SerializeField] private AttackData neutralAirData;
    [SerializeField] private AttackData upAirData;
    [SerializeField] private AttackData downAirData;
    [SerializeField] private AttackData forwardAirData;
    [SerializeField] private int maxAirHangs = 3;
    private int airHangsUsed;
    private bool isAirHanging;

    [Header("Grab")]
    [SerializeField] private AttackData grabAttackData;
    [SerializeField] private Vector2 holdOffset = new Vector2(0.8f, 1.2f);
    [SerializeField] private float grabHoldTime = 1.5f;
    [SerializeField] private float minHoldTime = 0.25f;
    private bool isGrabAttack;
    private Grabbable heldTarget;
    private float grabTimer;

    [Header("Throws")]
    [SerializeField] private ThrowData forwardThrow;
    [SerializeField] private ThrowData backThrow;
    [SerializeField] private ThrowData upThrow;
    [SerializeField] private ThrowData downThrow;
    [SerializeField] private float throwRecovery = 0.3f;
    private ThrowData pendingThrow;
    private bool pendingIsBackThrow;
    private bool isThrowing;
    private float throwTimer;
    private bool turnAfterThrow;


    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (comboTimer > 0f) comboTimer -= Time.deltaTime;
        if (playerMovement.IsGrounded) airHangsUsed = 0;
        HandleCombat();
        attackPressedThisFrame = false;
        grabPressedThisFrame = false;
    }

    void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            attackPressedThisFrame = true;
        }
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnGrab(InputValue value)
    {
        if (value.isPressed)
        {
            grabPressedThisFrame = true;
        }
    }

    void HandleCombat()
    {
        TryStartGrab();
        TryStartAttack();

        if (state == PlayerState.Attacking)
        {
            phaseTimer -= Time.deltaTime;

            HandleStartupPhase();
            HandleActivePhase();
            HandleRecoveryPhase();
            HandleQueuedInput();
        }
        if (state == PlayerState.Grabbing) HandleGrabbing();
        
    }

    void TryStartAttack()
    {
        if (state != PlayerState.Normal || !attackPressedThisFrame) return;

        isComboAttack = false;
        AttackData attack;

        if (playerMovement.CanDashAttack)
        {
            attack = dashAttackData;
            playerMovement.EndDash();
        }
        else if (!playerMovement.IsGrounded)
        {
            StartAirHang();
            attack = ChooseAirAttack();
        }
        else
        {
            attack = ChooseGroundAttack();
        }

        BeginAttack(attack, false);
        attackPressedThisFrame = false;
        Debug.Log("Playing: " + attack.attackName);
    }

    void TryStartGrab()
    {
        if (state != PlayerState.Normal || !grabPressedThisFrame) return;
        if (!playerMovement.IsGrounded || playerMovement.IsDashing) return;

        isComboAttack = false;
        BeginAttack(grabAttackData, true);
        grabPressedThisFrame = false;
    }

    AttackData ChooseAirAttack()
    {
        if (moveInput.y > 0.5f) return upAirData;
        if (moveInput.y < -0.5f) return downAirData;
        if (moveInput.x != 0) return forwardAirData;
        return neutralAirData;
    }

    AttackData ChooseGroundAttack()
    {
        if (moveInput.y > 0.5f) return upTiltData;
        if (moveInput.y < -0.5f) return downTiltData;

        bool continueCombo = comboTimer > 0f && comboStep > 0 && comboStep < comboAttacks.Length;
        comboStep = continueCombo ? comboStep + 1 : 1;
        isComboAttack = true;
        return comboAttacks[comboStep - 1];
    }

    void BeginAttack(AttackData attack, bool isGrab)
    {
        currentAttack = attack;
        isGrabAttack = isGrab;
        state = PlayerState.Attacking;
        phase = AttackPhase.Startup;
        phaseTimer = attack.startup;
        PlayAttackAnimation();
    }

    void HandleStartupPhase()
    {
        if (phase == AttackPhase.Startup && phaseTimer <= 0)
        {
            phase = AttackPhase.Active;
            phaseTimer = currentAttack.active;
            attackHitbox.SetActive(true);
            attackHitbox.transform.localPosition = currentAttack.hitboxOffset;
            attackHitbox.GetComponent<BoxCollider2D>().size = currentAttack.hitboxSize;
            hitTargetsThisSwing.Clear();
        }
    }

    void HandleActivePhase()
    {
        if (phase == AttackPhase.Active)
        {
            Vector2 hitboxSize = attackHitbox.GetComponent<BoxCollider2D>().size;
            Collider2D[] hits = Physics2D.OverlapBoxAll(attackHitbox.transform.position, hitboxSize, 0, hittableLayers);
            foreach (Collider2D hit in hits)
            {
                if (!hitTargetsThisSwing.Contains(hit))
                {
                    hitTargetsThisSwing.Add(hit);
                    Health targetHealth = hit.GetComponent<Health>();
                    if (targetHealth != null && targetHealth.IsInvincible) continue;
                    if (isGrabAttack)
                    {
                        Grabbable grabbable = hit.GetComponent<Grabbable>();
                        if (grabbable != null)
                        {
                            grabbable.Grab();
                            heldTarget = grabbable;
                            attackHitbox.SetActive(false);
                            state = PlayerState.Grabbing;
                            SetAnimation("Lars_grabIdle");
                            phase = AttackPhase.None;
                            grabTimer = 0;
                            return;
                        }
                    }
                    else
                    {
                        if (targetHealth != null)
                        {
                            Vector2 knockbackDirection;
                            if (currentAttack.launchDirection != Vector2.zero)
                                knockbackDirection = new Vector2(currentAttack.launchDirection.x * playerMovement.FacingDirection, currentAttack.launchDirection.y).normalized;
                            else
                                knockbackDirection = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                            targetHealth.TakeHit(currentAttack.damage, knockbackDirection, currentAttack.knockback, currentAttack.causesKnockdown);
                            StartCoroutine(HitStop());
                        }
                    }
                }
            }
            if (phaseTimer <= 0)
            {
                phase = AttackPhase.Recovery;
                phaseTimer = currentAttack.recovery;
                attackHitbox.SetActive(false);
            }
        }
    }

    void HandleRecoveryPhase()
    {
        if (phase == AttackPhase.Recovery && phaseTimer <= 0)
        {
            if (isComboAttack && queuedNextHit && comboStep < 3)
            {
                comboStep++;
                queuedNextHit = false;
                phase = AttackPhase.Startup;
                currentAttack = comboAttacks[comboStep - 1];
                PlayAttackAnimation();
                phaseTimer = currentAttack.startup;
                Debug.Log("Combo step " + comboStep + " — startup");
            }
            else
            {
                state = PlayerState.Normal;
                CurrentAnimation = null;
                if (turnAfterThrow)
                {
                    playerMovement.SetFacing(-playerMovement.FacingDirection);
                    turnAfterThrow = false;
                }
                StopAirHang();
                phase = AttackPhase.None;
                comboTimer = isComboAttack ? comboWindow : 0f;   // a short grace window to keep the combo going
                if (!isComboAttack) comboStep = 0;
                queuedNextHit = false;
                Debug.Log("Combo finished");
            }
        }
    }

    void HandleQueuedInput()
    {
        // Check for input to queue the next hit
        if (attackPressedThisFrame && phase != AttackPhase.None)
        {
            queuedNextHit = true;
            Debug.Log("Next hit queued");
        }
    }

    void HandleGrabbing()
    {
        if (heldTarget != null)
        {
            heldTarget.transform.position = (Vector2)transform.position + new Vector2(holdOffset.x * playerMovement.FacingDirection, holdOffset.y);
            if (isThrowing)
            {
                throwTimer -= Time.deltaTime;
                if (throwTimer <= 0f)
                {
                    isThrowing = false;
                    DoThrow(pendingThrow, pendingIsBackThrow);
                }
                return;
            }
            grabTimer += Time.deltaTime;
            if (grabTimer < minHoldTime) return;
            if (grabTimer >= grabHoldTime)
            {
                heldTarget.Release();
                heldTarget = null;
                state = PlayerState.Normal;
                CurrentAnimation = null;
                return;
            }
            if (moveInput.y > 0.5f)
            {
                // Debug.Log("Up throw");
                StartThrow(upThrow);
            }
            else if (moveInput.y < -0.5f)
            {
                // Debug.Log("Down throw");
                StartThrow(downThrow);
            }
            else if (moveInput.x != 0 && Mathf.Sign(moveInput.x) == playerMovement.FacingDirection)
            {
                // Debug.Log("Forward throw");
                StartThrow(forwardThrow);
            }
            else if (moveInput.x != 0 && Mathf.Sign(moveInput.x) == -playerMovement.FacingDirection)
            {
                // Debug.Log("Back throw");
                StartThrow(backThrow, true);
            }
            return;
        }
    }

    void DoThrow(ThrowData t, bool isBackThrow = false)
    {
        int oldFacing = playerMovement.FacingDirection;
        Vector2 dir = new Vector2(t.direction.x * oldFacing, t.direction.y).normalized;
        if (isBackThrow)
        {
            heldTarget.transform.position = (Vector2)transform.position + new Vector2(holdOffset.x * -oldFacing, holdOffset.y);
            turnAfterThrow = true;
        }
        heldTarget.Release();
        heldTarget.GetComponent<Health>().TakeHit(t.damage, dir, t.knockback, t.causesKnockdown);
        StartCoroutine(HitStop());
        heldTarget = null;
        state = PlayerState.Attacking;
        phase = AttackPhase.Recovery;
        phaseTimer = throwRecovery;
        isComboAttack = false;
        isGrabAttack = false;
    }

    void StartThrow(ThrowData t, bool isBackThrow = false)
    {
        pendingThrow = t;
        pendingIsBackThrow = isBackThrow;
        throwTimer = t.releaseDelay;
        isThrowing = true;
        SetAnimation(t.animationName);
    }

    void StartAirHang()
    {
        if (airHangsUsed >= maxAirHangs) return;
        playerMovement.SetAirHang(true);
        airHangsUsed++;
        isAirHanging = true;
    }

    void StopAirHang()
    {
        if (!isAirHanging) return;
        playerMovement.SetAirHang(false);
        isAirHanging = false;
    }

    IEnumerator HitStop()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }

    void PlayAttackAnimation()
    {
        SetAnimation(currentAttack.animationName);
    }

    void SetAnimation(string clip)
    {
        CurrentAnimation = clip;
        AttackId++;
    }

    public void Interrupt(PlayerState newState)
    {
        state = newState;
        phase = AttackPhase.None;
        comboStep = 0;
        queuedNextHit = false;
        attackHitbox.SetActive(false);
        isThrowing = false;
        StopAirHang();
        if (heldTarget != null) { heldTarget.Release(); heldTarget = null; }
        CurrentAnimation = null;
        AttackId++;   //makes the getHit clip restart on every new hit
        comboTimer = 0f;
    }

    void OnDrawGizmosSelected()
    {
        if (attackHitbox == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackHitbox.transform.position, attackHitbox.GetComponent<BoxCollider2D>().size);
    }
}