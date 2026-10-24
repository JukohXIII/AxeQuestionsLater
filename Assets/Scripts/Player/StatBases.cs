using System.Collections.Generic;
using UnityEngine;

public static class StatBases
{
    private static readonly Dictionary<StatType, float> bases = new();

    public static void Register(StatType stat, float value) => bases[stat] = value;
    public static bool TryGet(StatType stat, out float value) => bases.TryGetValue(stat, out value);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => bases.Clear();
}