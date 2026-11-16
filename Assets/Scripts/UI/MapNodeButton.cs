using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MapNodeButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    private const float SelectedScale = 1.2f;

    private static readonly Color ReachableColor = Color.white;
    private static readonly Color CurrentColor = new Color(1f, 0.85f, 0.3f);
    // Darkened instead of transparent, so the links behind it do not show through
    private static readonly Color LockedColor = new Color(0.4f, 0.4f, 0.45f);

    [SerializeField] private Image icon;                  // reward type; also the Button's target graphic
    [SerializeField] private Image stageIcon;             // optional: environment of the stage
    [SerializeField] private GameObject selectionFrame;   // shown while the node has focus
    
    [Header("Reward icons")]
    [SerializeField] private Sprite cardsIcon;
    [SerializeField] private Sprite mysteryIcon;
    [SerializeField] private Sprite healIcon;
    [SerializeField] private Sprite bossIcon;

    private Button button;

    public void Bind(MapNode node, bool reachable, bool isCurrent, Action<MapNode> onPick)
    {
        if (button == null) button = GetComponent<Button>();

        icon.sprite = IconFor(node.reward);
        icon.color = isCurrent ? CurrentColor : (reachable ? ReachableColor : LockedColor);

        if (stageIcon != null)
        {
            bool hasStageIcon = node.stage != null && node.stage.mapIcon != null;
            stageIcon.enabled = hasStageIcon;
            if (hasStageIcon)
            {
                stageIcon.sprite = node.stage.mapIcon;
                stageIcon.color = icon.color;
            }
        }

        button.interactable = reachable;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onPick(node));

        SetSelected(false); // the new state decides who has the focus, see MapScreen.Refresh
    }

    public void Select()
    {
        if (button == null) button = GetComponent<Button>();
        button.Select();
    }

    // Keyboard / gamepad focus
    public void OnSelect(BaseEventData eventData) => SetSelected(true);
    public void OnDeselect(BaseEventData eventData) => SetSelected(false);

    // Mouse: hovering a reachable node gives it the focus
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.interactable) button.Select();
    }

    void SetSelected(bool selected)
    {
        if (selectionFrame != null) selectionFrame.SetActive(selected);
        transform.localScale = Vector3.one * (selected ? SelectedScale : 1f);
    }

    Sprite IconFor(StageReward reward) => reward switch
    {
        StageReward.Cards => cardsIcon,
        StageReward.MysteryCard => mysteryIcon,
        StageReward.Heal => healIcon,
        _ => bossIcon
    };
}