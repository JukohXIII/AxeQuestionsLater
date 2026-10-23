using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string hubScene = "Hub";
    public void Show()
    {
        panel.SetActive(true);
    }

    public void Continue()
    {
        PlayerStats.ClearRun();   // run bonuses (cards) end with the run
        SceneManager.LoadScene(hubScene);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}