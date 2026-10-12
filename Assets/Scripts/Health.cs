using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float damagePercent = 0f;
    [Tooltip("Maximum damage at which the entity dies.")]
    [SerializeField] private float maxDamagePercent = 100f;
    private Rigidbody2D rb;

    public float DamagePercent => damagePercent;
    /// <summary> 
    /// Returns the damage ratio as a value between 0 and 1, where 0 means no damage and 1 means maximum damage, to use to scale visual health status feedback
    /// </summary>
    public float DamageRatio => Mathf.Clamp01(damagePercent / maxDamagePercent);
    public bool IsDead { get; private set; }
    //used to determine if the player should be knocked down or not from an attack
    public bool LastHitCausedKnockdown { get; private set; }
    //iframe used to prevent the player/the enemy from taking damage during knockdown or other states
    public bool IsInvincible { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Action OnHit;
    public Action OnDeath;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    public void TakeHit(float damageAmount, Vector2 knockbackDirection, float knockbackForce, bool causesKnockdown=false)
    {
        if (IsDead || IsInvincible) return;

        damagePercent += damageAmount;
        rb.linearVelocity = knockbackDirection * knockbackForce;
        LastHitCausedKnockdown = causesKnockdown;

        if (damagePercent >= maxDamagePercent)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
        else
        {
            OnHit?.Invoke();
        }
    }
}
