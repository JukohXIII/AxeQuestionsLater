using System.Collections.Generic;
using UnityEngine;

public static class UpgradeService
{
    private static UpgradeDefinition[] all;
    public static IReadOnlyList<UpgradeDefinition> All
    {
        get
        {
            if (all == null) all = Resources.LoadAll<UpgradeDefinition>("Upgrades");
            return all;
        }
    }

    // Runs before the first scene loads, so the bonuses exist in any scene,
    // even when the game starts from a stage without going through the hub
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Bootstrap()
    {
        all = null;
        ApplyAll();
    }

    // Rebuilds the permanent bonuses from the saved levels
    public static void ApplyAll()
    {
        PlayerStats.CLearPermanent();
        foreach(var def in All)
        {
            int level = GameProgress.GetLevel(def.id);
            if (level > 0)
                PlayerStats.AddPermanent(def.stat, def.mode, def.valuePerLevel * level);
        }
    }

    public static int GetLevel(UpgradeDefinition def) => GameProgress.GetLevel(def.id);
    public static bool IsMaxed(UpgradeDefinition def) => GetLevel(def) >= def.maxLevel;
    public static int GetNextCost(UpgradeDefinition def) => def.GetCost(GetLevel(def));
    public static bool CanBuy(UpgradeDefinition def) => !IsMaxed(def) && GameProgress.Souls >= GetNextCost(def);

    public static bool TryBuy(UpgradeDefinition def)
    {
        if (!CanBuy(def)) return false;

        int level = GetLevel(def);
        if (!GameProgress.TrySpend(def.GetCost(level))) return false;

        GameProgress.SetLevel(def.id, level + 1);
        GameProgress.Save();
        ApplyAll();
        return true;
    }
}