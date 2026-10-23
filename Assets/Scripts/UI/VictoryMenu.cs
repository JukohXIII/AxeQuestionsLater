using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class VictoryMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string mainMenuScene = "MainMenu";
    public bool IsShowing => panel.activeSelf;

    public void Show()
    {
        panel.SetActive(true);

        // Lars stays still: no more control, and remove the last stick direction
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<PlayerInput>().DeactivateInput();
        player.GetComponent<PlayerMovement>().ClearInput();
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}