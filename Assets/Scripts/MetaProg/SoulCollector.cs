using UnityEngine;

public class SoulCollector : MonoBehaviour
{
    private float remainder; 

    void OnEnable() => EnemyController.OnAnyEnemyDied += HandleEnemyDied;

    void OnDisable()
    {
        EnemyController.OnAnyEnemyDied -= HandleEnemyDied;
        GameProgress.Save();
    }

    void HandleEnemyDied(EnemyConfig config)
    {
        float gain = Mathf.Max(0f, PlayerStats.Apply(StatType.SoulGain, 1f));

        remainder += config.souls * gain;
        int whole = Mathf.FloorToInt(remainder);
        remainder -= whole;

        if (whole > 0) GameProgress.AddSouls(whole);
    }
}