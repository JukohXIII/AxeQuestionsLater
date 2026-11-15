using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float landDuration = 0.125f;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private PlayerAxe playerAxe;

    private string currentClip;
    private string lastCombatClip;
    private int lastAttackId;
    private bool wasGrounded = true;
    private float landTimer;
    private PlayerGetHit playerGetHit;
    private PlayerDefeat playerDefeat;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
        playerGetHit = GetComponent<PlayerGetHit>();
        playerDefeat = GetComponent<PlayerDefeat>();
        playerAxe = GetComponent<PlayerAxe>();
    }

    void Update()
    {
        // Start the landing pose the moment he touches the ground
        bool grounded = playerMovement.IsGrounded;
        if (grounded && !wasGrounded) landTimer = landDuration;
        wasGrounded = grounded;
        landTimer -= Time.deltaTime;

        string clip = ChooseClip();
        bool newAttack = playerCombat.AttackId != lastAttackId;
        lastAttackId = playerCombat.AttackId;

        if (clip != currentClip || newAttack)
        {
            bool versionSwap = !newAttack && currentClip == lastCombatClip && clip == playerCombat.CurrentAnimation;
            float start = versionSwap ? animator.GetCurrentAnimatorStateInfo(0).normalizedTime : 0f;
            animator.Play(clip, 0, start);
            currentClip = clip;
        }

        // STATS: only the attack clip follows the attack speed. Every other clip plays at normal speed.
        bool playingAttackClip = !string.IsNullOrEmpty(playerCombat.CurrentAnimation) && clip == playerCombat.CurrentAnimation;
        animator.speed = playingAttackClip ? playerCombat.AttackSpeedMultiplier : 1f;
        lastCombatClip = playerCombat.CurrentAnimation;
    }

    // Highest priority first
    string ChooseClip()
    {
        if (playerCombat.state == PlayerCombat.PlayerState.Defeated)
        {
            if (playerDefeat.IsRunningAway) return AxeClip("Lars_walk_withoutAxe");
                return playerDefeat.DefeatLanded ? "Lars_defeated" : "Lars_getHit";
        }
        if (playerCombat.state == PlayerCombat.PlayerState.Knockdown)
        {
            return playerGetHit.KnockdownLanded ? "Lars_getKnocked" : "Lars_getHit";
        }
        if (playerCombat.state == PlayerCombat.PlayerState.Hurt) return AxeClip("Lars_getHit");
        if (playerCombat.state == PlayerCombat.PlayerState.Blocking) return AxeClip("Lars_block");
        if (playerAxe.IsThrowing) return "Lars_throwAxe";
        if (!string.IsNullOrEmpty(playerCombat.CurrentAnimation)) return playerCombat.CurrentAnimation;
        if (playerAxe.IsCatching) return "Lars_receiveAxe";
        if (playerMovement.IsDashing) return AxeClip("Lars_dash");
        if (!playerMovement.IsGrounded) return rb.linearVelocity.y > 0.1f ? AxeClip("Lars_jump_up") : AxeClip("Lars_fall");
        if (landTimer > 0f) return AxeClip("Lars_land");
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) return AxeClip("Lars_walk");
        return AxeClip("Lars_idle");
    }

    string AxeClip(string clip) => playerAxe.HasAxe ? clip : clip + "_withoutAxe";
}