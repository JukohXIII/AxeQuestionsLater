using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string mainMenuScene = "MainMenu";

    public void Show()
    {
        panel.SetActive(true);
    }

    public void Continue()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);   // retry this stage
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}