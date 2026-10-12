using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UpgradeMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text soulsText;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private UpgradeRow rowPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<UpgradeRow> rows = new List<UpgradeRow>();
    private PlayerInteractor interactor;

    void Awake()
    {
        closeButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    void Update()
    {
        if (panel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Open(string title, StatUpgradeData[] upgrades, PlayerInteractor who)
    {
        interactor = who;
        interactor.SetControlLocked(true);

        titleText.text = title;

        foreach (var r in rows) Destroy(r.gameObject);
        rows.Clear();

        foreach (var u in upgrades)
        {
            var row = Instantiate(rowPrefab, rowsParent);
            row.Setup(u);
            rows.Add(row);
        }

        MetaProgression.OnChanged += Refresh;
        Refresh();
        panel.SetActive(true);
    }

    public void Close()
    {
        if (!panel.activeSelf) return;

        MetaProgression.OnChanged -= Refresh;
        panel.SetActive(false);
        interactor.SetControlLocked(false);
    }

    private void Refresh()
    {
        soulsText.text = $"Souls: {MetaProgression.Souls}";
        foreach (var r in rows) r.Refresh();
    }
}