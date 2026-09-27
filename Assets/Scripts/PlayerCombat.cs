using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using NUnit.Framework.Constraints;

[System.Serializable]
public class AttackData
{
    public string attackName;
    public float startup;
    public float active;
    public float recovery;
    public float damage;
    public float knockback;
    public Vector2 hitboxOffset = new Vector2(0.7f, 0f);
    public Vector2 hitboxSize = new Vector2(0.7f, 0.7f);
}

[System.Serializable]
public class ThrowData
{
    public float damage;
    public float knockback;
    public Vector2 direction;
}

public class PlayerCombat : MonoBehaviour
{

    public enum PlayerState { Normal, Attacking, Grabbing }
    public PlayerState state = PlayerState.Normal;
    private enum AttackPhase { None, Startup, Active, Recovery }
    private AttackPhase phase = AttackPhase.None;
    private bool attackPressedThisFrame;
    private float phaseTimer;
    private bool queuedNextHit;
    private int comboStep = 0;
    [SerializeField] private GameObject attackHitbox;
    [SerializeField] private LayerMask hittableLayers;
    private HashSet<Collider2D> hitTargetsThisSwing = new HashSet<Collider2D>();
    [SerializeField] private AttackData upTiltData;
    [SerializeField] private AttackData downTiltData;
    private AttackData currentAttack;
    private bool isComboAttack;
    private Vector2 moveInput;
    [SerializeField] private AttackData neutralAirData;
    [SerializeField] private AttackData upAirData;
    [SerializeField] private AttackData downAirData;
    [SerializeField] private AttackData forwardAirData;
    [SerializeField] private AttackData backAirData;
    private PlayerMovement playerMovement;
    [SerializeField] private AttackData dashAttackData;
    private bool grabPressedThisFrame;
    [SerializeField] private AttackData grabAttackData;
    private bool isGrabAttack;
    private Grabbable heldTarget;
    [SerializeField] private Vector2 holdOffset = new Vector2(0.8f, 1.2f);
    [SerializeField] private float grabHoldTime = 1.5f;
    [SerializeField] private float minHoldTime = 0.25f;
    private float grabTimer;
    [SerializeField] private ThrowData forwardThrow;
    [SerializeField] private ThrowData backThrow;
    [SerializeField] private ThrowData upThrow;
    [SerializeField] private ThrowData downThrow;
    [SerializeField] private float throwRecovery = 0.3f;

    [Header("Attack Settings")]
    [SerializeField] private AttackData[] comboAttacks;


    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }
    void Update()
    {
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
        if (state == PlayerState.Normal && attackPressedThisFrame)
        {
            if (playerMovement.CanDashAttack)
            {
                currentAttack = dashAttackData;
                isComboAttack = false;
                playerMovement.EndDash();
            }
            else if (!playerMovement.IsGrounded)
            {
                if (moveInput.y > 0.5f)
                {
                    currentAttack = upAirData;
                    isComboAttack = false;
                }
                else if (moveInput.y < -0.5f)
                {
                    currentAttack = downAirData;
                    isComboAttack = false;
                }
                else if (moveInput.x != 0 && playerMovement.FacingDirection == 1)
                {
                    currentAttack = forwardAirData;
                    isComboAttack = false;
                }
                else if (moveInput.x != 0 && playerMovement.FacingDirection == -1)
                {
                    currentAttack = backAirData;
                    isComboAttack = false;
                }
                else
                {
                    currentAttack = neutralAirData;
                    isComboAttack = false;
                }
            }
            else
            {
                if (moveInput.y > 0.5f)
                {
                    currentAttack = upTiltData;
                    isComboAttack = false;
                }
                else if (moveInput.y < -0.5f)
                {
                    currentAttack = downTiltData;
                    isComboAttack = false;
                }
                else
                {
                    comboStep = 1;
                    currentAttack = comboAttacks[comboStep - 1];
                    isComboAttack = true;
                }
            }
            state = PlayerState.Attacking;
            phase = AttackPhase.Startup;
            phaseTimer = currentAttack.startup;
            isGrabAttack = false;
            Debug.Log("Playing: " + currentAttack.attackName);
            Debug.Log("Combo step " + comboStep + " — startup");
        }
    }

    void TryStartGrab()
    {
        if (state == PlayerState.Normal && grabPressedThisFrame && playerMovement.IsGrounded && !playerMovement.IsDashing)
        {
            currentAttack = grabAttackData;
            isGrabAttack = true;
            state = PlayerState.Attacking;
            phase = AttackPhase.Startup;
            phaseTimer = currentAttack.startup;
            isComboAttack = false;
        }
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
                    if (isGrabAttack)
                    {
                        Grabbable grabbable = hit.GetComponent<Grabbable>();
                        if (grabbable != null)
                        {
                            grabbable.Grab();
                            heldTarget = grabbable;
                            attackHitbox.SetActive(false);
                            state = PlayerState.Grabbing;
                            phase = AttackPhase.None;
                            grabTimer = 0;
                            return;
                        }
                    }
                    else
                    {
                        if (targetHealth != null)
                        {
                            Vector2 knockbackDirection = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                            targetHealth.TakeHit(currentAttack.damage, knockbackDirection, currentAttack.knockback);
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
                phaseTimer = currentAttack.startup;
                Debug.Log("Combo step " + comboStep + " — startup");
            }
            else
            {
                state = PlayerState.Normal;
                phase = AttackPhase.None;
                comboStep = 0;
                queuedNextHit = false;
                Debug.Log("Combo finished");
            }
        }
    }

    void HandleQueuedInput()
    {
        // Check for input to queue the next hit
        if (attackPressedThisFrame && (phase == AttackPhase.Active || phase == AttackPhase.Recovery))
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
            grabTimer += Time.deltaTime;
            if (grabTimer < minHoldTime) return;
            if (grabTimer >= grabHoldTime)
            {
                heldTarget.Release();
                heldTarget = null;
                state = PlayerState.Normal;
                return;
            }
            if (moveInput.y > 0.5f)
            {
                DoThrow(upThrow);
            }
            else if (moveInput.y < -0.5f)
            {
                DoThrow(downThrow);
            }
            else if (moveInput.x != 0 && Mathf.Sign(moveInput.x) == playerMovement.FacingDirection)
            {
                DoThrow(forwardThrow);
            }
            else if (moveInput.x != 0 && Mathf.Sign(moveInput.x) == -playerMovement.FacingDirection)
            {
                DoThrow(backThrow, true);
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
            playerMovement.SetFacing(-oldFacing);
            heldTarget.transform.position = (Vector2)transform.position + new Vector2(holdOffset.x * -oldFacing, holdOffset.y);
        }
        heldTarget.Release();
        heldTarget.GetComponent<Health>().TakeHit(t.damage, dir, t.knockback);
        heldTarget = null;
        state = PlayerState.Attacking;
        phase = AttackPhase.Recovery;
        phaseTimer = throwRecovery;
        isComboAttack = false;
        isGrabAttack = false;
    }

    void OnDrawGizmosSelected()
    {
        if (attackHitbox == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackHitbox.transform.position, attackHitbox.GetComponent<BoxCollider2D>().size);
    }
}