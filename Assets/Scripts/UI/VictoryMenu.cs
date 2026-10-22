using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string mainMenuScene = "MainMenu";

    public void Show()
    {
        panel.SetActive(true);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}