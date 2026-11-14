using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGuard : MonoBehaviour
{
    [SerializeField] private float blockstunDuration = 0.2f;
    [SerializeField] private float maxStamina = 6;
    [SerializeField] private float regenRate = 1.8f;
    [SerializeField] private float regenDelay = 0.5f;
    [SerializeField] private float recoverThreshold = 0.34f;


    // ---------- References ----------
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;
    private Health health;
    private InputAction blockAction;
    private PlayerAxe playerAxe;

    // ---------- State ----------
    public bool IsInBlockstun => blockstunTimer > 0f;
    private float blockstunTimer;
    float stamina;
    float regenTimer;
    bool guardBroken;
    public float StaminaRatio => stamina / maxStamina;
    public bool IsGuardBroken => guardBroken;
    public float MaxStamina => maxStamina;
    public float Stamina => stamina;

    void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        health = GetComponent<Health>();
        blockAction = GetComponent<PlayerInput>().actions["Block"];
        stamina = maxStamina;
        playerAxe = GetComponent<PlayerAxe>();
    }

    void OnEnable()  { health.OnBlocked += GuardHit; }
    void OnDisable() { health.OnBlocked -= GuardHit; }

    void Update()
    {
        bool held = blockAction.IsPressed();
        bool canBlock = !guardBroken && playerAxe.HasAxe;

        if (blockstunTimer > 0f)
        {
            blockstunTimer -= Time.deltaTime;
            held = true;   // can't drop the guard during blockstun
        }
        if (held && canBlock)
        {
            playerCombat.ClearAttackBuffer();   // can't queue an attack while holding guard
            playerMovement.ClearDashBuffer();
        }

        var state = playerCombat.state;
        if (state == PlayerCombat.PlayerState.Normal && held && playerMovement.IsGrounded && canBlock)
            playerCombat.state = PlayerCombat.PlayerState.Blocking;
        else if (state == PlayerCombat.PlayerState.Blocking && (!held || !playerMovement.IsGrounded || (guardBroken && !IsInBlockstun)))
            playerCombat.state = PlayerCombat.PlayerState.Normal;

        health.GuardFacing = playerCombat.state == PlayerCombat.PlayerState.Blocking ? playerMovement.FacingDirection : 0;

        if (state != PlayerCombat.PlayerState.Blocking)
        {
            regenTimer -= Time.deltaTime;
            if (regenTimer <= 0f)
            {
                stamina += regenRate * Time.deltaTime;
                stamina = Mathf.Min(stamina, maxStamina);
            }
            if (guardBroken && stamina >= maxStamina * recoverThreshold)
            {
                guardBroken = false;
            }
        }
    }

    void StartBlockstun()
    {
        blockstunTimer = blockstunDuration;
    }

    void GuardHit()
    {
        StartBlockstun();
        stamina -= health.LastGuardCost;
        regenTimer = regenDelay;
        if (stamina <= 0f)
        {
            stamina = 0f;
            guardBroken = true;
            
        }
    }
}