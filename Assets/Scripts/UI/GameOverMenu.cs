using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private GameObject firstButton;

    public void Show()
    {
        panel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(firstButton);
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