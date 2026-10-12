using UnityEngine;
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.VisualScripting;
using NUnit.Framework;

public class MetaProgression
{
    public static readonly object ModifierSource = new();
    public static event Action OnChanged;
    
    private static SaveData data;
    private static Dictionary<string, StatUpgradeData> statUpgradeDataCatalog;

    private static void EnsureLoaded()
    {
        if (data != null) return;

        data = SaveSystem.Load();

        statUpgradeDataCatalog = new Dictionary<string, StatUpgradeData>();

        // Every UpgradeData asset must live in Assets/Resources/Upgrades/
        foreach (var upgrade in Resources.LoadAll<StatUpgradeData>("Upgrades"))
            statUpgradeDataCatalog[upgrade.id] = upgrade;      
    }

    public static int Souls
    {
        get { EnsureLoaded(); return data.souls; }
    }

    public static int GetLevel(StatUpgradeData upgrade)
    {
        EnsureLoaded();
        var entry = data.upgrades.Find(e => e.id == upgrade.id);
        return entry != null ? entry.level : 0;
    }

    public static bool IsMaxed(StatUpgradeData upgradeData) => GetLevel(upgradeData) >= upgradeData.maxLevel;
    public static int NextCost(StatUpgradeData upgradeData) => upgradeData.CostAtLevel(GetLevel(upgradeData));
    public static bool CanBuy(StatUpgradeData upgradeData) => !IsMaxed(upgradeData) && Souls >= NextCost(upgradeData);

    public static bool TryBuy(StatUpgradeData upgradeData)
    {
        if (!CanBuy(upgradeData)) return false;

        data.souls -= NextCost(upgradeData);

        var entry = data.upgrades.Find(e => e.id == upgradeData.id);
        if(entry == null)
        {
            entry = new UpgradeLevel { id = upgradeData.id };
            data.upgrades.Add(entry);
        }
        entry.level++;

        Commit();
        return true;
    }

    public static void AddSouls(int amount)
    {
        EnsureLoaded();
        data.souls += amount;
        Commit();
    }

    public static void ResetAll()
    {
        EnsureLoaded();
        data = new SaveData();
        Commit();
    }

    public static void ApplyTo(PlayerStats stats)
    {
        EnsureLoaded();
        stats.RemoveModifiersFrom(ModifierSource);

        foreach(var entry in data.upgrades)
        {
            if (entry.level <= 0 || !statUpgradeDataCatalog.TryGetValue(entry.id, out var statUpgradeData)) continue;
            stats.AddModifier(new StatModifier(statUpgradeData.statType, statUpgradeData.modifierKind, statUpgradeData.valuePerLevel * entry.level, ModifierSource));
        }
    }

    private static void Commit()
    {
        SaveSystem.Save(data);
        OnChanged.Invoke();
    }
}
