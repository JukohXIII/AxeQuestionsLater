using System;
using UnityEngine;
using System.Collections.Generic;

public enum StatType
{
    // Mirror (Character)
    MaxHealth, MoveSpeed, AirJumps, DashCooldown, SoulChance, SoulMultiplier,
    // Sharpening stone (Axe)
    Damage, AttackSpeed, ComboSpeed, ComboWindow,
    LifeSteal,
    KnockbackTaken
}

public enum ModifierMode { Flat, Percent }

public static class PlayerStats
{
    private class Layer
    {
        private readonly Dictionary<StatType, float> flat = new ();
        private readonly Dictionary<StatType, float> percent = new ();

        public void Add(StatType stat, ModifierMode mode, float value)
        {
            var table = mode == ModifierMode.Flat ? flat : percent;
            table.TryGetValue(stat, out float current);
            table[stat] = current + value;
        }

        public float Flat(StatType stat) => flat.TryGetValue(stat, out float v) ? v : 0f;
        public float Percent(StatType stat) => percent.TryGetValue(stat, out float v) ? v : 0f;
        public void Clear() { flat.Clear(); percent.Clear(); }
    }

    // Permanent: hub upgrades, rebuilt from the save. Run: cards, cleared when the run ends.
    private static readonly Layer permanent = new ();
    private static readonly Layer run = new ();

    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        permanent.Clear();
        run.Clear();
        Changed = null;
    }

    // (base + flat bonuses) * (1 + sum of percent bonuses). Never below 0.
    public static float Apply(StatType statType, float baseValue)
    {
        float flat = permanent.Flat(statType) + run.Flat(statType);
        float percent = permanent.Percent(statType) + run.Percent(statType);
        return (baseValue + flat) * Mathf.Max(0f, 1f + percent);
    }

    public static float Preview(StatType stat, float baseValue, float addFlat, float addPercent)
    {
        float flat = permanent.Flat(stat) + run.Flat(stat) + addFlat;
        float percent = permanent.Percent(stat) + run.Percent(stat) + addPercent;
        return (baseValue + flat) * Mathf.Max(0f, 1f + percent);
    }

    public static void AddPermanent(StatType stat, ModifierMode mode, float value)
    {
        permanent.Add(stat, mode, value);
        Changed?.Invoke();
    }

    public static void AddRun(StatType stat, ModifierMode mode, float value)
    {
        run.Add(stat, mode, value);
        Changed?.Invoke();
    }

    public static void CLearPermanent() { permanent.Clear(); Changed?.Invoke(); }
    public static void ClearRun() { run.Clear(); Changed?.Invoke(); }
}