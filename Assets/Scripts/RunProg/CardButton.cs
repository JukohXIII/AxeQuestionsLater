using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CardButton : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private TMP_Text stackLabel;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text effectsLabel;
    private Button button;

    public void Bind(CardDefinition card, System.Action<CardDefinition> onPick)
    {
        if (button == null) button = GetComponent<Button>();

        nameLabel.text = card.displayName;
        nameLabel.color = RarityRules.TextColor(card.rarity);
        descriptionLabel.text = card.description;
        if (effectsLabel != null) effectsLabel.text = card.EffectsText();
        
        int owned = RunCards.GetStacks(card);
        stackLabel.text = (card.maxStacks == 1 || owned == 0) ? "" : $"x{owned} > x{owned + 1}";

        if (icon != null)
        {
            icon.sprite = card.icon;
            icon.enabled = card.icon != null;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onPick(card));
    }
}