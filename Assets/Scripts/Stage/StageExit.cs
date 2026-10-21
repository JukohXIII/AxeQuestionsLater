using UnityEngine;

public class StageExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private ScreenTransition fader;
    private bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        var combat = other.GetComponent<PlayerCombat>();
        if (combat != null && combat.state == PlayerCombat.PlayerState.Defeated) return;
        triggered = true;
        fader.FadeToScene(nextSceneName);
    }
}