using System;
using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]

[RequireComponent(typeof(PlayerStats))]
public class Health : MonoBehaviour
{
    public float DamagePercent => damagePercent;
    public float DamageRatio => Mathf.Clamp01(damagePercent / MaxDamage);
    public bool IsDead { get; private set; }
    //used to determine if the player should be knocked down or not from an attack
    
    public bool LastHitCausedKnockdown { get; private set; }
    //iframe used to prevent the player/the enemy from taking damage during knockdown or other states
    public bool IsInvincible { get; set; }
    public Action OnHit;
    public Action OnDeath;
    [SerializeField] private float damagePercent = 0f;
    [Tooltip("Maximum damage at which the entity dies.")]
    [SerializeField] private float maxDamagePercent = 100f;
    private Rigidbody2D rb;
    private PlayerStats stats;
    private float MaxDamage => stats != null ? stats.AbsoluteApply(StatType.MaxHealth, maxDamagePercent) : maxDamagePercent;
    private float DefenseMult =>
        stats != null ? stats.MultApply(StatType.Defense) : 1f;
    private float KnockbackResistMult =>
        stats != null ? stats.MultApply(StatType.KnockbackResistance) : 1f;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
    }
    public void TakeHit(float damageAmount, Vector2 knockbackDirection, float knockbackForce, bool causesKnockdown=false)
    {
        if (IsDead || IsInvincible) return;

        damagePercent += damageAmount / DefenseMult;
        rb.linearVelocity = knockbackForce * knockbackDirection / KnockbackResistMult;
        LastHitCausedKnockdown = causesKnockdown;

        if (damagePercent >= MaxDamage)
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
