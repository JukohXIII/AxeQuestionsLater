using System.Collections;
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
    private bool selectionLocked;

    void Awake() => closeButton.onClick.AddListener(Close);

    public void Open(UpgradeCategory category, string title)
    {
        gameObject.SetActive(true);
        AudioManager.Play(Sfx.PanelOpen);
        titleLabel.text = title;

        foreach (var row in rows) Destroy(row.gameObject);
        rows.Clear();

        foreach (var def in UpgradeService.All.Where(d => d.category == category).OrderBy(d => d.sortOrder))
        {
            var row = Instantiate(rowPrefab, rowContainer);
            row.Bind(def, this);
            rows.Add(row);
        }

        LockPlayer();

        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);

        selectionLocked = true;            // pas de sélection tant que la touche d'ouverture est dans cette frame
        Refresh();
        StartCoroutine(SelectNextFrame());
    }

    IEnumerator SelectNextFrame()
    {
        yield return null;
        selectionLocked = false;
        EnsureSelection();
    }

    public void Close()
    {
        AudioManager.Play(Sfx.UiClick);
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        selectionLocked = false;
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
        BuildNavigation();
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

    void BuildNavigation()
    {
        var chain = rows.Where(r => r.BuyButton.gameObject.activeInHierarchy)
                        .Select(r => r.BuyButton)
                        .ToList();
        chain.Add(closeButton);

        for (int i = 0; i < chain.Count; i++)
        {
            chain[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = i > 0 ? chain[i - 1] : null,
                selectOnDown = i < chain.Count - 1 ? chain[i + 1] : null,
            };
        }
    }
    void EnsureSelection()
    {
        if (selectionLocked) return;

        var es = EventSystem.current;
        if (es == null) return;

        Selectable current = es.currentSelectedGameObject != null
            ? es.currentSelectedGameObject.GetComponent<Selectable>()
            : null;

        if (current == null || !current.IsInteractable() || !current.gameObject.activeInHierarchy)
            es.SetSelectedGameObject(FirstInteractable());
    }

    GameObject FirstInteractable()
    {
        foreach (var row in rows)
            if (row.BuyButton.interactable && row.BuyButton.gameObject.activeInHierarchy)
                return row.BuyButton.gameObject;
        return closeButton.gameObject;
    }
}