using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CardEffect
{
    public StatType stat;
    public ModifierMode mode;
    public float value;
}

[CreateAssetMenu(menuName = "Game/Card", fileName = "Card_")]
public class CardDefinition : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public List<CardEffect> effects = new();
    [Min(0)]public int maxStacks = 1;
    [Min(0f)] public float weight = 1f;
    public CardRarity rarity = CardRarity.Common;
    public string EffectsText()
    {
        var stats = new List<StatType>();
        foreach (var e in effects)
            if (!stats.Contains(e.stat)) stats.Add(e.stat);

        var sb = new System.Text.StringBuilder();
        foreach (var stat in stats)
        {
            float addFlat = 0f, addPercent = 0f;
            foreach (var e in effects)
            {
                if (e.stat != stat) continue;
                if (e.mode == ModifierMode.Flat) addFlat += e.value;
                else addPercent += e.value;
            }
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(StatText.Preview(stat, addFlat, addPercent));
        }
        return sb.ToString();
    }
}