using UnityEngine;
using System;
using System.Collections.Generic;

public enum StatType
{
    // Combat
    Damage, AttackSpeed, ComboSpeed, KnockbackDealt, AttackRange, GrabHoldTime,
    // Defense
    MaxHealth, Defense, KnockbackResistance,
    // Movement
    MoveSpeed, JumpPower, ExtraAirJumps, DashRecovery, DashSpeed
}

public enum ModifierKind { Flat, Percent }

public class StatModifier
{
    public readonly StatType stat;
    public readonly ModifierKind kind;
    public readonly float value; // For percent : 0.1 = +10%
    public readonly object source; // Who added it (stage reward, souls spent... )

    public StatModifier(StatType theStat, ModifierKind theKind, float theValue, object theSource)
    {
        stat = theStat;
        kind = theKind;
        value = theValue;
        source = theSource;
    }
}

public class PlayerStats : MonoBehaviour
{
   private readonly List<StatModifier> modifiers = new List<StatModifier>();
   public event Action OnStatsChanged;

    // For stats expressed in absolute units (max health, damage): (base + flat) * (1 + percent)
    public float AbsoluteApply(StatType theStatType, float theBaseValue)
    {
        float flat = 0f, percent = 0f;
        foreach(var modifier in modifiers)
        {
            if(modifier.stat != theStatType) continue;
            if(modifier.kind != ModifierKind.Flat) flat += modifier.value;
            else percent += modifier.value;
        }
        return (theBaseValue + flat) * (1f + percent);
    }

    // For stats used as a multiplier (move speed, attack speed): 1 + percent. Flat is ignored.
    public float MultApply(StatType theStatType)
    {
        float percent = 0f;
        foreach (var modifier in modifiers)
        {
            if(modifier.stat == theStatType && modifier.kind == ModifierKind.Percent)
                percent += modifier.value;
        }
        return Mathf.Max(0.1f, 1f + percent);
    }

    public void AddModifier(StatModifier theModifier)
    {
        modifiers.Add(theModifier);
        OnStatsChanged?.Invoke();
    }

    public void RemoveModifiersFrom(object theSource)
    {
        if(modifiers.RemoveAll(modifier => modifier.source == theSource) > 0)
            OnStatsChanged?.Invoke();
    }
}
