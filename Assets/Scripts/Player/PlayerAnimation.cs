using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        bool isWalking = Mathf.Abs(rb.linearVelocity.x) > 0.1f && playerMovement.IsGrounded;
        animator.SetBool("isGrounded", playerMovement.IsGrounded);
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isDashing", playerMovement.IsDashing);
    }
}