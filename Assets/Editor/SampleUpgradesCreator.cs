using UnityEditor;
using UnityEngine;

public static class SampleUpgradesCreator
{
    private const string Folder = "Assets/Resources/Upgrades";

    [MenuItem("Tools/Create Sample Upgrades")]
    private static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Upgrades");

        Create("Vitality",  "Vitality",  "Increases max health.",    StatType.MaxHealth,   0.05f, 10, 50, 1.35f);
        Create("Strength",  "Strength",  "Increases damage dealt.",  StatType.Damage,      0.05f, 10, 60, 1.35f);
        Create("Quickness", "Quickness", "Attacks come out faster.", StatType.AttackSpeed, 0.04f, 10, 75, 1.4f);
        Create("Flow",      "Flow",      "Combos are faster.",       StatType.ComboSpeed,  0.05f, 5, 100, 1.5f);
        Create("Fleetfoot", "Fleetfoot", "Move faster.",             StatType.MoveSpeed,   0.03f, 8, 40, 1.3f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void Create(string fileName, string displayName, string description,
        StatType stat, float valuePerLevel, int maxLevel, int baseCost, float costGrowth)
    {
        string path = $"{Folder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<StatUpgradeData>(path) != null) return;

        var u = ScriptableObject.CreateInstance<StatUpgradeData>();
        u.id = fileName;
        u.displayName = displayName;
        u.description = description;
        u.statType = stat;
        u.modifierKind = ModifierKind.Percent;
        u.valuePerLevel = valuePerLevel;
        u.maxLevel = maxLevel;
        u.baseCost = baseCost;
        u.costGrowth = costGrowth;

        AssetDatabase.CreateAsset(u, path);
    }
}