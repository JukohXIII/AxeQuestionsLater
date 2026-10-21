using UnityEngine;

public class PlayerGetHit : MonoBehaviour
{
    [SerializeField] private float hitstunDuration = 0.3f;
    [SerializeField] private float knockdownDuration = 1.2f;

    // ---------- References ----------
    private Health health;
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;

    // ---------- State ----------
    public bool KnockdownLanded => knockdownLanded;
    private float timer;
    private bool knockdownLanded;
    private float landCheckDelay;


    void Awake()
    {
        health = GetComponent<Health>();
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        SetPassThroughEnemies(false);
    }

    void OnEnable()  { health.OnHit += HandleHit; }
    void OnDisable() { health.OnHit -= HandleHit; }

    void HandleHit()
    {
        SetPassThroughEnemies(true);   // so he doesn't get stuck on enemies while in hitstun
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

    void Update()
    {
        if (playerCombat.state == PlayerCombat.PlayerState.Defeated) return;
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
        SetPassThroughEnemies(false);
        playerCombat.Interrupt(PlayerCombat.PlayerState.Normal);
    }

    void SetPassThroughEnemies(bool on)
    {
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), on);
    }
}