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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
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
        if (!string.IsNullOrEmpty(playerCombat.CurrentAnimation)) return playerCombat.CurrentAnimation;
        if (playerMovement.IsDashing) return "Lars_dash";
        if (!playerMovement.IsGrounded) return rb.linearVelocity.y > 0.1f ? "Lars_jump_up" : "Lars_fall";
        if (landTimer > 0f) return "Lars_land";
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) return "Lars_walk";
        return "Lars_idle";
    }
}