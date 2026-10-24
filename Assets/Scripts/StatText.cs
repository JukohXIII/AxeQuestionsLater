using UnityEngine;

public static class StatText
{
    public static string Name(StatType stat) => stat switch
    {
        StatType.MaxHealth      => "Max health",
        StatType.MoveSpeed      => "Move speed",
        StatType.AirJumps       => "Air jumps",
        StatType.DashCooldown   => "Dash cooldown",
        StatType.SoulChance     => "Soul drop chance",
        StatType.SoulMultiplier => "Souls dropped",
        StatType.Damage         => "Damage",
        StatType.AttackSpeed    => "Attack speed",
        StatType.ComboSpeed     => "Combo speed",
        StatType.ComboWindow    => "Combo window",
        StatType.LifeSteal      => "Life steal",
        StatType.KnockbackTaken => "Knockback taken",
        _ => stat.ToString()
    };

    // Pour ces stats, une valeur plus basse est un avantage
    static bool LowerIsBetter(StatType s) =>
        s == StatType.DashCooldown || s == StatType.KnockbackTaken;

    // Sans base déclarée : on affiche le bonus total en % (base 1 = multiplicateur, 0 = fraction)
    static float RelativeBase(StatType s) =>
        (s == StatType.SoulChance || s == StatType.LifeSteal) ? 0f : 1f;

    static string Absolute(StatType s, float v) => s switch
    {
        StatType.DashCooldown or StatType.ComboWindow => $"{v:0.00}s",
        StatType.AirJumps => Mathf.RoundToInt(v).ToString(),
        _ => $"{v:0.#}"
    };

    static string Percent(float v) => $"{v * 100f:+0.#;-0.#;0}%";

    public static string Preview(StatType stat, float addFlat, float addPercent)
    {
        bool hasBase = StatBases.TryGet(stat, out float baseValue);
        float b = hasBase ? baseValue : RelativeBase(stat);

        float current = PlayerStats.Apply(stat, b);
        float next = PlayerStats.Preview(stat, b, addFlat, addPercent);

        string from = hasBase ? Absolute(stat, current) : Percent(current - b);
        string to   = hasBase ? Absolute(stat, next)    : Percent(next - b);

        string color = Mathf.Approximately(current, next) ? "#FFFFFF"
            : ((next > current) != LowerIsBetter(stat) ? "#7CE38B" : "#FF7A7A");

        return $"{Name(stat)}  {from} > <color={color}>{to}</color>";
    }
}