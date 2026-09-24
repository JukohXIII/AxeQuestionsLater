using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float damagePercent = 0f;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    public void TakeHit(float damageAmount, Vector2 knockbackDirection, float knockbackForce)
    {
        damagePercent += damageAmount;
        rb.linearVelocity = knockbackDirection * knockbackForce;
    }
}
