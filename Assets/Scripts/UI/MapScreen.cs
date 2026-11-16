using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapScreen : MonoBehaviour
{
    private const float FloorSpacing = 200f;
    private const float NodeSpacing = 140f;
    private const float LineThickness = 6f;
    [SerializeField] private RectTransform container; 
    [SerializeField] private MapNodeButton nodePrefab;
    [SerializeField] private Image linePrefab;
    [SerializeField] private ScreenTransition fader;
    [SerializeField] private TMP_Text statusLabel;

    private readonly Dictionary<MapNode, MapNodeButton> buttons = new();
    private bool leaving;
    private MapNodeButton firstReachable;   // fallback focus

    void Start()
    {
        if (!RunMap.IsGenerated) RunMap.Generate();

        if (RunMap.Current == null)
            Debug.Log(RunMap.Describe());   // first time the map is shown for this run

        Build();
        Refresh();
    }

    void Build()
    {
        foreach (var floor in RunMap.Floors)
        {
            foreach (var node in floor)
            {
                foreach (var next in node.next)
                    DrawLine(PositionOf(node), PositionOf(next));

                var button = Instantiate(nodePrefab, container);
                ((RectTransform)button.transform).anchoredPosition = PositionOf(node);
                buttons[node] = button;
            }
        }
    }

    // Lines are created first and pushed behind the nodes
    void DrawLine(Vector2 a, Vector2 b)
    {
        Image line = Instantiate(linePrefab, container);
        RectTransform rect = line.rectTransform;
        rect.anchoredPosition = (a + b) / 2f;
        rect.sizeDelta = new Vector2(Vector2.Distance(a, b), LineThickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
        rect.SetAsFirstSibling();
    }

    Vector2 PositionOf(MapNode node)
    {
        int floorCount = RunMap.Floors.Count;
        int nodesInFloor = RunMap.Floors[node.floor].Count;
        float x = (node.floor - (floorCount - 1) / 2f) * FloorSpacing;
        float y = ((nodesInFloor - 1) / 2f - node.index) * NodeSpacing;
        return new Vector2(x, y);
    }

    void Refresh()
    {
        firstReachable = null;

        foreach (var pair in buttons)
        {
            bool reachable = RunMap.CanChoose(pair.Key);
            pair.Value.Bind(pair.Key, reachable, pair.Key == RunMap.Current, OnPick);
            if (reachable && firstReachable == null) firstReachable = pair.Value;
        }

        if (firstReachable != null) firstReachable.Select();

        if (statusLabel != null)
        {
            statusLabel.gameObject.SetActive(RunData.PlayerMaxHealth > 0f);
            statusLabel.text = $"Health {RunData.PlayerMaxHealth - RunData.PlayerDamage:0} / {RunData.PlayerMaxHealth:0}";
        }
    }

    // Navigation does nothing without a selected object (e.g. after a click on the background): restore the focus
    void Update()
    {
        if (leaving || firstReachable == null) return;
        if (EventSystem.current.currentSelectedGameObject == null)
            firstReachable.Select();
    }

    void OnPick(MapNode node)
    {
        if (leaving || !RunMap.Choose(node)) return;

        if (node.reward == StageReward.Heal)
        {
            RunData.HealPlayer(RunData.HealNodeFraction);
            Refresh();
            return;
        }

        leaving = true;
        fader.FadeToScene(node.stage.sceneName);
    }
}