using UnityEngine;

public class SoulCollector : MonoBehaviour
{
    private const string PrefabPath = "SoulPickup"; // Assets/Resources/SoulPickup.prefab
    private float remainder; 
    private SoulPickup prefab;
    private Collider2D playerCollider;

    void Awake() => prefab = Resources.Load<SoulPickup>(PrefabPath);

    void OnEnable() => EnemyController.OnAnyEnemyDied += HandleEnemyDied;

    void OnDisable()
    {
        EnemyController.OnAnyEnemyDied -= HandleEnemyDied;
        GameProgress.Save();
    }

    void HandleEnemyDied(EnemyConfig config, Vector2 deathPosition)
    {
        float gain = Mathf.Max(0f, PlayerStats.Apply(StatType.SoulGain, 1f));

        remainder += config.souls * gain;
        int whole = Mathf.FloorToInt(remainder);
        remainder -= whole;
        if (whole <= 0) return;

        if (playerCollider == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null) playerCollider = player.GetComponent<Collider2D>();
        }

        // Fallback: no prefab or no player found, credit directly like before
        if (prefab == null || playerCollider == null)
        {
            GameProgress.AddSouls(whole);
            AudioManager.Play(Sfx.Soul);
            return;
        }

        // One orb carries all the souls of the enemy
        Instantiate(prefab, deathPosition, Quaternion.identity).Init(playerCollider, whole);
    }
}