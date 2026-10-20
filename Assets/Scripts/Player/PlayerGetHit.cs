using UnityEngine;

public class PlayerGetHit : MonoBehaviour
{
    [SerializeField] private float hitstunDuration = 0.3f;
    [SerializeField] private float knockdownDuration = 1.2f;

    private Health health;
    private PlayerCombat playerCombat;
    private float timer;
    private PlayerMovement playerMovement;
    private bool knockdownLanded;
    private float landCheckDelay;
    public bool KnockdownLanded => knockdownLanded;

    void Awake()
    {
        health = GetComponent<Health>();
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void OnEnable()  { health.OnHit += HandleHit; health.OnDeath += HandleDeath; }
    void OnDisable() { health.OnHit -= HandleHit; health.OnDeath -= HandleDeath; }

    void HandleHit()
    {
        if (health.LastHitCausedKnockdown)
        {
            playerCombat.Interrupt(PlayerCombat.PlayerState.Knockdown);
            timer = knockdownDuration;
            knockdownLanded = false;   // invincibility starts when he lands
            landCheckDelay = 0.2f;
        }
        else
        {
            playerCombat.Interrupt(PlayerCombat.PlayerState.Hurt);
            timer = hitstunDuration;
        }
    }

    void HandleDeath()
    {
        playerCombat.Interrupt(PlayerCombat.PlayerState.Defeated);
    }

    void Update()
    {
        var state = playerCombat.state;
        if (state != PlayerCombat.PlayerState.Hurt && state != PlayerCombat.PlayerState.Knockdown) return;
        if (state == PlayerCombat.PlayerState.Knockdown && !knockdownLanded)
        {
            landCheckDelay -= Time.deltaTime;
            if (landCheckDelay > 0f || !playerMovement.IsGrounded) return;
            knockdownLanded = true;
            health.IsInvincible = true;
            timer = knockdownDuration;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (state == PlayerCombat.PlayerState.Knockdown) health.IsInvincible = false;
        playerCombat.Interrupt(PlayerCombat.PlayerState.Normal);
    }
}