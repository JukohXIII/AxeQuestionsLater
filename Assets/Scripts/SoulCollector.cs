using System;
using UnityEngine;

public class SoulCollector : MonoBehaviour
{
    [Header("Base values (upgrades add to these)")]
    [Range(0f, 1f)] [SerializeField] private float baseChance = 0f;
    [SerializeField] private float baseMultiplier = 2f;
    [Tooltip("Hard cap on the chance, whatever the upgrades.")]
    [Range(0f, 1f)] [SerializeField] private float maxChance = 0.5f;

    void OnEnable() => EnemyController.OnAnyEnemyDied += HandleEnemyDied;

    void OnDisable()
    {
        EnemyController.OnAnyEnemyDied -= HandleEnemyDied;
        GameProgress.Save();
    }

    void HandleEnemyDied(EnemyConfig config)
    {
        int amount = config.souls;

        float chance = Mathf.Min(PlayerStats.Apply(StatType.SoulChance, baseChance), maxChance);
        if (UnityEngine.Random.value < chance)
        {
            float multiplier = Mathf.Min(PlayerStats.Apply(StatType.SoulMultiplier, baseMultiplier));
            amount = Mathf.RoundToInt(amount * multiplier);
        }

        GameProgress.AddSouls(amount);
    }
}