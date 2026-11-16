using UnityEngine;

public class StageExit : MonoBehaviour
{
    private bool triggered;
    [Tooltip("Used when no map exists (a stage opened on its own for testing).")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private ScreenTransition fader;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        var health = other.GetComponent<Health>();
        var combat = other.GetComponent<PlayerCombat>();
        if (combat != null && combat.state == PlayerCombat.PlayerState.Defeated) return;
        triggered = true;
        RunData.PlayerDamage = health.DamagePercent;
        RunData.PlayerMaxHealth = health.MaxHealth;
        fader.FadeToScene(DestinationScene());
    }

    string DestinationScene() => RunMap.IsGenerated ? RunMap.MapScene : nextSceneName;
}