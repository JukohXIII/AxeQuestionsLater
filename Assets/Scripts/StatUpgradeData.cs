using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Upgrades/Upgrade")]
public class StatUpgradeData : ScriptableObject
{
    [Tooltip("Unique key used in the save file. Don't rename it once saves exist.")]
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("Effect (per level)")]
    public StatType statType;
    public ModifierKind modifierKind = ModifierKind.Percent;
    [Tooltip("Percent 0.05 = +5% per level. Flat: added as is.")]
    public float valuePerLevel = 0.05f;
    public int maxLevel = 10;

    [Header("Cost")]
    public int baseCost = 50;
    public float costGrowth = 1.35f;

    public int CostAtLevel(int currentLevel) =>
        Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, currentLevel));

    public string FormatTotal(int level)
    {
        float total = valuePerLevel*level;
        return modifierKind == ModifierKind.Percent ? $"{total * 100f:0.#}%" : $"+{total:0.#}";
    }

    void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) 
            id = name;
    }
}
