using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private TMP_Text effectLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private Image icon;   // optional
    [SerializeField] private Button buyButton;

    private UpgradeDefinition definition;
    private UpgradePanel panel;

    public Button BuyButton => buyButton;
    
    public void Bind(UpgradeDefinition def, UpgradePanel owner)
    {
        definition = def;
        panel = owner;

        nameLabel.text = def.displayName;
        if (icon != null)
        {
            icon.sprite = def.icon;
            icon.enabled = def.icon != null;
        }
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    void OnBuyClicked()
    {
        UpgradeService.TryBuy(definition);
        panel.Refresh(); // the level changes after the souls are spent, so refresh explicitly
    }

    public void Refresh()
    {
        int level = UpgradeService.GetLevel(definition);
        bool maxed = UpgradeService.IsMaxed(definition);
        buyButton.gameObject.SetActive(!maxed);
        
        levelLabel.text = $"{level}/{definition.maxLevel}";
        effectLabel.text = maxed
            ? $"{definition.FormatBonus(level)} (MAX)"
            : $"{definition.FormatBonus(level)} > {definition.FormatBonus(level + 1)}";
        costLabel.text = maxed ? "MAX" : UpgradeService.GetNextCost(definition).ToString();
        buyButton.interactable = UpgradeService.CanBuy(definition);
    }
}