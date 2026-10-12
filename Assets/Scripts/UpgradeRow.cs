using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buyButton;

    private StatUpgradeData data;

    public void Setup(StatUpgradeData upgrade)
    {
        data = upgrade;
        nameText.text = upgrade.displayName;
        buyButton.onClick.AddListener(() => MetaProgression.TryBuy(data));
    }

    public void Refresh()
    {
        int level = MetaProgression.GetLevel(data);
        levelText.text = $"{level}/{data.maxLevel}";
        effectText.text = data.FormatTotal(level);
        costText.text = MetaProgression.IsMaxed(data) ? "MAX" : MetaProgression.NextCost(data).ToString();
        buyButton.interactable = MetaProgression.CanBuy(data);
    }
}