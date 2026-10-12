using UnityEngine;
using UnityEngine.InputSystem;

public class TiptoolPanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject movesPanel;
    [SerializeField] private PlayerInput playerInput;

    public void ToggleMovesPanel()
    {
        bool isOpening = !movesPanel.activeSelf;
        movesPanel.SetActive(isOpening);
        Time.timeScale = isOpening ? 0f : 1f;

        if (isOpening) playerInput.DeactivateInput();
        else playerInput.ActivateInput();
    }
}
