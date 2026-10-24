using UnityEngine;

public enum CardRarity { Common, Rare, Epic, Legendary }

public static class RarityRules
{
    private static readonly float[] baseWeight = { 1.00f, 0.35f, 0.10f, 0.03f};
    private static readonly float[] luckScale  = { 0f, 2f, 4f, 8f};

    public static float Weight(CardRarity rarity, float luck)
    {
        int i = (int)rarity;
        return baseWeight[i] * (1f + Mathf.Max(0f, luck) * luckScale[i]);
    }

    public static Color TextColor(CardRarity rarity) => rarity switch
    {
        CardRarity.Rare      => new Color(0.45f, 0.70f, 1.00f),   // #73B3FF
        CardRarity.Epic      => new Color(0.78f, 0.50f, 1.00f),   // #C780FF
        CardRarity.Legendary => new Color(1.00f, 0.65f, 0.20f),   // #FFA633
        _ => Color.white
    };
}