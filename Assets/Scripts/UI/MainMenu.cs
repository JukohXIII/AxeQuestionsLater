using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string firstStageScene = "Stage 1";

    void Start()
    {
        Time.timeScale = 1f;   // safety: un-pause in case we came back from a paused game
    }

    public void StartGame()
    {
        RunData.PlayerDamage = 0f;
        SceneManager.LoadScene(firstStageScene);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // Quit does nothing in the Editor, so stop Play mode instead
#endif
    }
}