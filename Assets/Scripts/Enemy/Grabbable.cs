using UnityEngine;

public class Grabbable : MonoBehaviour
{
    private Rigidbody2D rb;
    private RigidbodyType2D originalBodyType;
    private float originalGravityScale;
    private Collider2D col;
    public bool IsGrabbed { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalBodyType = rb.bodyType;
        originalGravityScale = rb.gravityScale;
        col = GetComponent<Collider2D>();
    }

    [ContextMenu("Grab")]
    public void Grab()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        col.isTrigger = true;
        IsGrabbed = true;
    }

    [ContextMenu("Release")]
    public void Release()
    {
        rb.bodyType = originalBodyType;
        rb.gravityScale = originalGravityScale;
        col.isTrigger = false;
        IsGrabbed = false;
    }
}
