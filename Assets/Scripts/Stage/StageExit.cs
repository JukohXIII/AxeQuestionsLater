using UnityEngine;

public class StageExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private ScreenTransition fader;
    private bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;
        fader.FadeToScene(nextSceneName);
    }
}