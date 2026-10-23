using UnityEngine;

public enum UpgradeCategory { Character, Weapon }

[CreateAssetMenu(fileName = "Upgrade_", menuName = "Upgrades/Upgrade Definition")]
public class UpgradeDefinition : ScriptableObject
{
    [Tooltip("Unique, stable key used in the save file. Never rename it once there is progress")]
    public string id;
    public UpgradeCategory category;

    [Header("Display")]
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effect")]
    public StatType stat;
    public ModifierMode mode = ModifierMode.Percent;
    [Tooltip("Bonus per level. Percent: 0.1 = +10%. Flat: added to the base value. Negative for a reduction.")]
    public float valuePerLevel = 0.1f;
    public int maxLevel = 5;

    [Header("Cost")]
    public int baseCost = 20;
    [Tooltip("Cost of level n+1 = baseCost * growth^n")]
    public float costGrowth = 1.4f;

    public int GetCost(int currentLevel) => Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, currentLevel));

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            Debug.LogWarning($"{name}: upgrade has no id, it can't be saved.", this);
    }
}