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
    public Vector2 hitboxOffset = new (0.7f, 0f);
    public Vector2 hitboxSize = new (0.7f, 0.7f);
    public bool causesKnockdown;
    public bool heavyHit;
    [Range(0f, 1f)] public float cancelPoint = 0.4f;
    public float lungeSpeed = 0f;
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

    public enum PlayerState { Normal, Attacking, Grabbing, Blocking, Hurt, Knockdown, Defeated, AxeThrow }
    private enum AttackPhase { None, Startup, Active, Recovery }

    // ---------- References ----------
    private PlayerMovement playerMovement;
    private PauseMenu pauseMenu;
    private Health health;
    private PlayerAxe playerAxe;

    // ---------- State (other scripts read these) ----------
    [Header("State")]
    public PlayerState state = PlayerState.Normal;
    public string CurrentAnimation { get; private set; }
    public int AttackId { get; private set; }
    public bool IsHurt => state == PlayerState.Hurt || state == PlayerState.Knockdown || state == PlayerState.Defeated;
    public float AttackSpeedMultiplier { get; private set; } = 1f;

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
    float queuedTimer;
    Vector2 queuedMove;
    [SerializeField] private float attackBufferTime = 0.2f;
    [SerializeField] private float lateSwapWindow = 0.08f;
    private int comboStep;
    [SerializeField] private float comboWindow = 0.3f;   // time after a jab to still continue the combo
    
    [Tooltip("Global tempo for all attacks. Below 1 = slower. Upgrades stack on top.")]
    [SerializeField] private float baseAttackSpeed = 0.85f;
    private float comboTimer;
    private float recoveryTime;

    // For enemy AI: reading the player's attack phase
    public bool IsInAttackRecovery =>
        state == PlayerState.Attacking && phase == AttackPhase.Recovery;

    public bool CanCancel =>
        state == PlayerState.Attacking && phase == AttackPhase.Recovery && phaseTimer <= recoveryTime * (1f - currentAttack.cancelPoint);
    
    public bool CanDashCancel =>
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
    [SerializeField] private float closeLungeMultiplier = 0.5f;

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
        pauseMenu = FindAnyObjectByType<PauseMenu>();
        health = GetComponent<Health>();
        playerAxe = GetComponent<PlayerAxe>();

        StatBases.Register(StatType.ComboWindow, comboWindow);
    }

    void Update()
    {
        if (comboTimer > 0f) comboTimer -= Time.deltaTime;
        if (queuedTimer > 0f) queuedTimer -= Time.deltaTime;
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
        if (!playerAxe.HasAxe) return;
        if (state != PlayerState.Normal && !CanCancel) return;
        if (!attackPressedThisFrame && queuedTimer <= 0f) return;

        if (CanCancel)
        {
            EndAttack();
        }

        isComboAttack = false;
        AttackData attack;

        if (playerMovement.CanDashAttack && playerMovement.IsGrounded)
        {
            attack = dashAttackData;
            playerMovement.EndDash();
        }
        else if (!playerMovement.IsGrounded)
        {
            if (playerMovement.IsDashing)
            {
                playerMovement.EndDash();
            }
            StartAirHang();
            attack = ChooseAirAttack();
        }
        else
        {
            attack = ChooseGroundAttack();
        }

        BeginAttack(attack, false);
        queuedMove = Vector2.zero;
        attackPressedThisFrame = false;
        Debug.Log("Playing: " + attack.attackName);
    }

    void TryStartGrab()
    {
        if (!playerAxe.HasAxe) return;
        if (state != PlayerState.Normal || !grabPressedThisFrame) return;
        if (!playerMovement.IsGrounded || playerMovement.IsDashing) return;

        isComboAttack = false;
        BeginAttack(grabAttackData, true);
        grabPressedThisFrame = false;
    }

    Vector2 AttackDirection()
    {
        // Use the stick now; if it's neutral, use the direction saved with the buffered press
        return moveInput.magnitude > 0.5f ? moveInput : queuedMove;
    }
    AttackData ChooseAirAttack()
    {
        Vector2 dir = AttackDirection();
        if (dir.y > 0.5f) return upAirData;
        if (dir.y < -0.5f) return downAirData;
        if (dir.x != 0) return forwardAirData;
        return neutralAirData;
    }

    AttackData ChooseGroundAttack()
    {
        Vector2 dir = AttackDirection();
        if (dir.y > 0.5f) return upTiltData;
        if (dir.y < -0.5f) return downTiltData;

        bool continueCombo = comboTimer > 0f && comboStep > 0 && comboStep < comboAttacks.Length;
        comboStep = continueCombo ? comboStep + 1 : 1;
        isComboAttack = true;
        return comboAttacks[comboStep - 1];
    }

    void UpdateAttackSpeed()
    {
        if (isGrabAttack)
            AttackSpeedMultiplier = 1f;
        else
            AttackSpeedMultiplier = PlayerStats.Apply(isComboAttack ? StatType.ComboSpeed : StatType.AttackSpeed, 1f) * baseAttackSpeed;

        AttackSpeedMultiplier = Mathf.Max(AttackSpeedMultiplier, 0.1f); // never 0: we divide by it
    }

    void BeginAttack(AttackData attack, bool isGrab)
    {
        currentAttack = attack;
        isGrabAttack = isGrab;
        UpdateAttackSpeed();
        state = PlayerState.Attacking;
        phase = AttackPhase.Startup;
        phaseTimer = attack.startup / AttackSpeedMultiplier;
        PlayAttackAnimation();
        Vector2 checkPos = (Vector2)transform.position + new Vector2(attack.hitboxOffset.x * playerMovement.FacingDirection, attack.hitboxOffset.y);
        bool enemyInRange = Physics2D.OverlapBox(checkPos, attack.hitboxSize, 0f, hittableLayers);
        if (!isGrab && attack.lungeSpeed > 0f)
        {
            float speed = enemyInRange ? attack.lungeSpeed * closeLungeMultiplier : attack.lungeSpeed;
            playerMovement.Lunge(speed);
        }
        
        if (!isGrab) PlaySwingSound();
    }

    void HandleStartupPhase()
    {
        if (phase == AttackPhase.Startup && phaseTimer <= 0)
        {
            phase = AttackPhase.Active;
            phaseTimer = currentAttack.active / AttackSpeedMultiplier;
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
                            AudioManager.Play(Sfx.Grab);
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
                            float dealt = PlayerStats.Apply(StatType.Damage, currentAttack.damage);
                            targetHealth.TakeHit(dealt, knockbackDirection, currentAttack.knockback, currentAttack.causesKnockdown);
                            ApplyLifeSteal(dealt);
                            AudioManager.Play(currentAttack.heavyHit ? Sfx.HitHeavy : Sfx.Hit);
                            StartCoroutine(HitStop(hitStopDuration / AttackSpeedMultiplier));
                        }
                    }
                }
            }
            if (phaseTimer <= 0)
            {
                recoveryTime = currentAttack.recovery / AttackSpeedMultiplier;
                phase = AttackPhase.Recovery;
                phaseTimer = currentAttack.recovery / AttackSpeedMultiplier;
                attackHitbox.SetActive(false);
            }
        }
    }

    void HandleRecoveryPhase()
    {
        if (phase == AttackPhase.Recovery && phaseTimer <= 0)
        {
            EndAttack();
        }
    }

    void HandleQueuedInput()
    {
        if (!attackPressedThisFrame || phase == AttackPhase.None) return;

        if (phase == AttackPhase.Startup)
        {
            float elapsed = currentAttack.startup / AttackSpeedMultiplier - phaseTimer;
            bool tiltInput = Mathf.Abs(moveInput.y) > 0.5f;
            if (elapsed <= lateSwapWindow && isComboAttack && tiltInput && playerMovement.IsGrounded)
                SwapToTilt();
            return;   // any other press during startup is dropped
        }

        queuedTimer = attackBufferTime;
        queuedMove = moveInput;
    }

    void SwapToTilt()
    {
        isComboAttack = false;        // the tilt ends the jab combo
        playerMovement.Lunge(0f);     // cancel the jab's forward step
        BeginAttack(moveInput.y > 0 ? upTiltData : downTiltData, false);
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
        AttackSpeedMultiplier = 1f;
        int oldFacing = playerMovement.FacingDirection;
        Vector2 dir = new Vector2(t.direction.x * oldFacing, t.direction.y).normalized;
        if (isBackThrow)
        {
            heldTarget.transform.position = (Vector2)transform.position + new Vector2(holdOffset.x * -oldFacing, holdOffset.y);
            turnAfterThrow = true;
        }
        heldTarget.Release();
        if (heldTarget.TryGetComponent(out ThrownCollateral collateral)) collateral.Launch();
        float dealt = PlayerStats.Apply(StatType.Damage, t.damage);
        heldTarget.GetComponent<Health>().TakeHit(dealt, dir, t.knockback, t.causesKnockdown);
        ApplyLifeSteal(dealt);
        AudioManager.Play(Sfx.HitHeavy);
        StartCoroutine(HitStop(hitStopDuration));
        heldTarget = null;
        state = PlayerState.Attacking;
        recoveryTime = throwRecovery;
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
        AudioManager.Play(Sfx.Throw);
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

    IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        if (pauseMenu == null || !pauseMenu.IsPaused)
            Time.timeScale = 1f;
    }

    void PlayAttackAnimation()
    {
        SetAnimation(currentAttack.animationName);
        PlaySwingSound();
    }

    void SetAnimation(string clip)
    {
        CurrentAnimation = clip;
        AttackId++;
    }

    public void Interrupt(PlayerState newState)
    {
        if (playerMovement.IsDashing) playerMovement.EndDash();
        state = newState;
        phase = AttackPhase.None;
        comboStep = 0;
        queuedTimer = 0f;
        queuedMove = Vector2.zero;
        attackHitbox.SetActive(false);
        isThrowing = false;
        StopAirHang();
        if (heldTarget != null) { heldTarget.Release(); heldTarget = null; }
        CurrentAnimation = null;
        AttackId++;   //makes the getHit clip restart on every new hit
        comboTimer = 0f;
    }

    void ApplyLifeSteal(float damageDealt)
    {
        float ratio = PlayerStats.Apply(StatType.LifeSteal, 0f);
        if (ratio > 0f) health.Heal(damageDealt * ratio);
    }

    void PlaySwingSound()
    {
        AudioManager.Play(currentAttack.heavyHit ? Sfx.SwingHeavy : Sfx.Swing);
    }

    public void EndAttack()
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
        comboTimer = isComboAttack ? PlayerStats.Apply(StatType.ComboWindow, comboWindow) : 0f;    // a short grace window to keep the combo going
        if (!isComboAttack) comboStep = 0;
        queuedTimer = 0f;
        Debug.Log("Combo finished");
    }

    public void ClearAttackBuffer() 
    {
        queuedTimer = 0f;
        queuedMove = Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        if (attackHitbox == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackHitbox.transform.position, attackHitbox.GetComponent<BoxCollider2D>().size);
    }
}