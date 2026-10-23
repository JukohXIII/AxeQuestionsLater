using UnityEngine;
using UnityEngine.InputSystem;

public class TiptoolPanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject movesPanel;
    [SerializeField] private PlayerInput playerInput;

    void Awake()
    {
        if (playerInput == null)
            playerInput = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInput>();
    }

    bool EndScreenOpen()
    {
        VictoryMenu victory = FindAnyObjectByType<VictoryMenu>();
        return victory != null && victory.IsShowing;
    }

    public void ToggleMovesPanel()
    {
        bool isOpening = !movesPanel.activeSelf;
        movesPanel.SetActive(isOpening);
        Time.timeScale = isOpening ? 0f : 1f;

        if (isOpening) playerInput.DeactivateInput();
        else if (!playerInput.GetComponent<Health>().IsDead && !EndScreenOpen()) playerInput.ActivateInput();
    }
}
