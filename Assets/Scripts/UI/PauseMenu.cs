using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.Samples.RebindUI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject pauseFirstButton;     // Resume
    [SerializeField] private GameObject controlsFirstButton;  // first rebind button (Move Up)

    private InputAction pauseAction;
    private PlayerInput playerInput;
    private Health playerHealth;
    private InputAction backAction;

    public bool IsPaused => panel.activeSelf || controlsPanel.activeSelf;

    void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerInput = player.GetComponent<PlayerInput>();
        playerHealth = player.GetComponent<Health>();

        // Its own action, so it still works while Lars's controls are switched off
        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
        pauseAction.performed += _ => TogglePause();

        backAction = new InputAction("Back", InputActionType.Button, "<Gamepad>/buttonEast");
        backAction.performed += _ => Back();
    }

    void OnEnable()  { pauseAction.Enable();  backAction.Enable(); }
    void OnDisable() { pauseAction.Disable(); backAction.Disable(); }
    void OnDestroy() { pauseAction.Dispose(); backAction.Dispose(); }

    public void TogglePause()
    {
        if (IsRebinding()) return;
        if (controlsPanel.activeSelf) { CloseControls(); return; }
        if (IsPaused) Resume();
        else Pause();
    }

    void Pause()
    {
        if (playerHealth.IsDead || EndScreenOpen()) return;   // no pausing on Game Over or Victory
        panel.SetActive(true);
        Time.timeScale = 0f;
        playerInput.DeactivateInput();
        Select(pauseFirstButton);
    }

    public void Resume()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
        playerInput.ActivateInput();
        controlsPanel.SetActive(false);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;   // don't carry the pause into the menu
        SceneManager.LoadScene(mainMenuScene);
    }

    bool EndScreenOpen()
    {
        VictoryMenu victory = FindAnyObjectByType<VictoryMenu>();
        return victory != null && victory.IsShowing;
    }

    public void OpenControls()
    {
        panel.SetActive(false);
        controlsPanel.SetActive(true);
        Select(controlsFirstButton);
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
        panel.SetActive(true);
        Select(pauseFirstButton);
    }

    void Select(GameObject button)
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button);
    }

    void Back()
    {
        if (IsRebinding()) return;
        if (controlsPanel.activeSelf) CloseControls();
        else if (panel.activeSelf) Resume();
    }

    bool IsRebinding()
    {
        foreach (var row in controlsPanel.GetComponentsInChildren<RebindActionUI>())
            if (row.ongoingRebind != null) return true;
        return false;
    }
}