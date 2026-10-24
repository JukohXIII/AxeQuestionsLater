using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class VictoryMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private GameObject firstButton;
    public bool IsShowing => panel.activeSelf;

    public void Show()
    {
        panel.SetActive(true);

        // Lars stays still: no more control, and remove the last stick direction
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<PlayerInput>().DeactivateInput();
        player.GetComponent<PlayerMovement>().ClearInput();
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}