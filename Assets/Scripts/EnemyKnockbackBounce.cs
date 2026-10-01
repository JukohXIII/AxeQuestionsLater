using UnityEngine;

public class EnemyKnockbackBounce : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float bounceDamping = 0.4f;
    [SerializeField] private float minBounceSpeed = 5f;
    private Rigidbody2D rb;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayers) != 0)
        {
            if (rb.linearVelocity.y < -minBounceSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -rb.linearVelocity.y * bounceDamping);
            }
        }
    }
}