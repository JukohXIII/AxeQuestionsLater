using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float landDuration = 0.125f;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;

    private string currentClip;
    private int lastAttackId;
    private bool wasGrounded = true;
    private float landTimer;
    private PlayerGetHit playerGetHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
        playerGetHit = GetComponent<PlayerGetHit>();
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
            animator.Play(clip, 0, 0f);
            currentClip = clip;
        }
    }

    // Highest priority first
    string ChooseClip()
    {
        if (playerCombat.state == PlayerCombat.PlayerState.Defeated) return "Lars_defeated";
        if (playerCombat.state == PlayerCombat.PlayerState.Knockdown)
        {
            return playerGetHit.KnockdownLanded ? "Lars_getKnocked" : "Lars_getHit";
        }
        if (playerCombat.state == PlayerCombat.PlayerState.Hurt) return "Lars_getHit";
        if (playerCombat.state == PlayerCombat.PlayerState.Blocking) return "Lars_block";
        if (!string.IsNullOrEmpty(playerCombat.CurrentAnimation)) return playerCombat.CurrentAnimation;
        if (playerMovement.IsDashing) return "Lars_dash";
        if (!playerMovement.IsGrounded) return rb.linearVelocity.y > 0.1f ? "Lars_jump_up" : "Lars_fall";
        if (landTimer > 0f) return "Lars_land";
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) return "Lars_walk";
        return "Lars_idle";
    }
}