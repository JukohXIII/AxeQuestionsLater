using UnityEngine;

// Pushes stats into components that hold state (Health), instead of reading them at each use
[RequireComponent(typeof(Health))]
public class PlayerStatsApplier : MonoBehaviour
{
    private Health health;
    private float baseMaxDamagePercent;

    void Awake()
    {
        health = GetComponent<Health>();
        baseMaxDamagePercent = health.MaxHealth;
        StatBases.Register(StatType.MaxHealth, baseMaxDamagePercent);
    }

    void OnEnable()
    {
        PlayerStats.Changed += Refresh;
        Refresh();
    }

    void OnDisable() => PlayerStats.Changed -= Refresh;

    void Refresh()
    {
        health.SetMaxDamagePercent(PlayerStats.Apply(StatType.MaxHealth, baseMaxDamagePercent));
        health.KnockbackTakenMultiplier = PlayerStats.Apply(StatType.KnockbackTaken, 1f);
    }
}