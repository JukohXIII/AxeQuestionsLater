using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text soulsLabel;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private UpgradeRow rowPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private PlayerInput playerInput;

    private readonly List<UpgradeRow> rows = new List<UpgradeRow>();

    void Awake() => closeButton.onClick.AddListener(Close);

    public void Open(UpgradeCategory category, string title)
    {
        gameObject.SetActive(true);
        titleLabel.text = title;

        foreach (var row in rows) Destroy(row.gameObject);
        rows.Clear();

        foreach(var def in UpgradeService.All.Where(d => d.category == category).OrderBy(d => d.sortOrder))
        {
            var row = Instantiate(rowPrefab, rowContainer);
            row.Bind(def, this);
            rows.Add(row);
        }

        LockPlayer();

        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);
        Refresh();
    } 

    public void Close() => gameObject.SetActive(false);

    void OnDisable()
    {
        if (playerInput != null) playerInput.ActivateInput();
    }

    void Update()
    {
        bool cancel = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                        || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (cancel) Close();
    }

    public void Refresh()
    {
        soulsLabel.text = $"Souls: {GameProgress.Souls}";
        foreach (var row in rows) row.Refresh();
        EnsureSelection();
    }

    void LockPlayer()
    {
        if (playerInput == null) return;
        playerInput.DeactivateInput();

        // Deactivating doesn't zero the last move input
        var movement = playerInput.GetComponent<PlayerMovement>();
        if (movement != null) movement.ClearInput();
    }

    // Keeps a valid selection for gamepad navigation (a button that becomes disabled
    void EnsureSelection()
    {
        var es = EventSystem.current;
        if (es == null) return;

        Selectable current = es.currentSelectedGameObject != null
            ? es.currentSelectedGameObject.GetComponent<Selectable>()
            : null;

        if (current == null || !current.IsInteractable())
            es.SetSelectedGameObject(FirstInteractable());
    }

    GameObject FirstInteractable()
    {
        foreach (var row in rows)
            if (row.BuyButton.interactable) return row.BuyButton.gameObject;
        return closeButton.gameObject;
    }
}