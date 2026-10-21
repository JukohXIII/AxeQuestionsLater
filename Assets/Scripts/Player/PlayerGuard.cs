using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGuard : MonoBehaviour
{
    [SerializeField] private float blockstunDuration = 0.2f;

    // ---------- References ----------
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;
    private Health health;
    private InputAction blockAction;

    // ---------- State ----------
    public bool IsInBlockstun => blockstunTimer > 0f;
    private float blockstunTimer;

    void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        health = GetComponent<Health>();
        blockAction = GetComponent<PlayerInput>().actions["Block"];
    }

    void OnEnable()  { health.OnBlocked += StartBlockstun; }
    void OnDisable() { health.OnBlocked -= StartBlockstun; }

    void Update()
    {
        bool held = blockAction.IsPressed();

        if (blockstunTimer > 0f)
        {
            blockstunTimer -= Time.deltaTime;
            held = true;   // can't drop the guard during blockstun
        }

        var state = playerCombat.state;
        if (state == PlayerCombat.PlayerState.Normal && held && playerMovement.IsGrounded)
            playerCombat.state = PlayerCombat.PlayerState.Blocking;
        else if (state == PlayerCombat.PlayerState.Blocking && (!held || !playerMovement.IsGrounded))
            playerCombat.state = PlayerCombat.PlayerState.Normal;

        health.GuardFacing = playerCombat.state == PlayerCombat.PlayerState.Blocking ? playerMovement.FacingDirection : 0;
    }

    void StartBlockstun()
    {
        blockstunTimer = blockstunDuration;
    }
}