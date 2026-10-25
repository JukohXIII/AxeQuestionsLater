using System.Collections.Generic;
using UnityEngine;

public static class RunCards
{
    public static event System.Action Changed;
    
    private static readonly Dictionary<CardDefinition, int> stacks = new();
    private static CardDefinition[] all;

    private static CardDefinition[] All => all ??= Resources.LoadAll<CardDefinition>("Cards");

    public static int GetStacks(CardDefinition card) => stacks.TryGetValue(card, out int n) ? n : 0;

    public static bool CanTake(CardDefinition card) =>
        card.maxStacks <= 0 || GetStacks(card) < card.maxStacks;

    public static void Take(CardDefinition card)
    {
        if (!CanTake(card)) return;
        stacks[card] = GetStacks(card) + 1;
        foreach (var e in card.effects)
            PlayerStats.AddRun(e.stat, e.mode, e.value);
        Changed?.Invoke();
    }

    public static void Clear()
    {
        stacks.Clear();
        PlayerStats.ClearRun();
        Changed?.Invoke();
    }

    public static List<CardDefinition> Draw(int count)
    {
        float luck = PlayerStats.Apply(StatType.Luck, 0f);
        float Weight(CardDefinition c) => c.weight * RarityRules.Weight(c.rarity, luck);

        var pool = new List<CardDefinition>();
        foreach (var c in All)
            if (c.weight > 0f && CanTake(c)) pool.Add(c);

        var result = new List<CardDefinition>();
        while (result.Count < count && pool.Count > 0)
        {
            float total = 0f;
            foreach (var c in pool) total += Weight(c);

            float roll = UnityEngine.Random.value * total;
            int pick = pool.Count - 1;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Weight(pool[i]);
                if (roll <= 0f) { pick = i; break; }
            }
            result.Add(pool[pick]);
            pool.RemoveAt(pick);
        }
        return result;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        stacks.Clear();
        all = null;
        Changed = null;
    }    
}