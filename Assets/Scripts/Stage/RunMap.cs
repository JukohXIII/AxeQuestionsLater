using System.Collections.Generic;
using UnityEngine;

// The map of the current run: generated once at the start, survives scene changes.
public static class RunMap
{
    private const int FloorCount = 6; // start stage + 4 choices + boss
    private const int MinNodesPerfloor = 2;
    private const int MaxNodesPerFloor = 3;

    private const float CardsWeight = 70f;
    private const float MysteryCardWeight = 30f;
    private const int FirstHealFloor = 2; // the heal node can be on floors 2 to FloorCount - 2

    private static readonly List<List<MapNode>> floors = new();

    public static IReadOnlyList<List<MapNode>> Floors => floors;
    public static MapNode Current { get; private set; }
    public static bool IsGenerated => floors.Count > 0;
    public const string MapScene = "Map";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Clear();

    public static void Clear()
    {
        floors.Clear();
        Current = null;
    }

    public static void Generate(IReadOnlyList<StageDefinition> stagePool, StageDefinition bossStage)
    {
        Clear();

        for (int f = 0; f < FloorCount; f++)
        {
            bool isStart = f == 0;
            bool isBoss = f == FloorCount - 1;
            int count = (isStart || isBoss) ? 1 : Random.Range(MinNodesPerfloor, MaxNodesPerFloor + 1);

            var floor = new List<MapNode>();
            for (int i = 0; i < count; i++)
            {
                var node = new MapNode(f, i);
                if (isBoss)
                {
                    node.reward = StageReward.Boss;
                    node.stage = bossStage;
                }
                else
                {
                    node.reward = isStart ? StageReward.Cards : PickReward();
                    node.stage = stagePool[Random.Range(0, stagePool.Count)];
                }
                floor.Add(node);
            }
            floors.Add(floor);
        }

        PlaceHealNode();

        for (int f = 0; f < FloorCount - 1; f++)
            Connect(floors[f], floors[f + 1]);
        
        Current = null;   // nothing played yet: the player picks the first node on the map
    }

    public static void Generate()
    {
        var settings = Resources.Load<RunMapSettings>("RunMapSettings");
        if (settings == null || settings.stagePool.Length == 0 || settings.bossStage == null)
        {
            Debug.LogError("RunMap: RunMapSettings is missing in Resources or incomplete.");
            return;
        }
        Generate(settings.stagePool, settings.bossStage);
    }

    // Before the first stage there is no current node: the player picks one on floor 0
    public static bool CanChoose(MapNode node) =>
        Current == null ? node.floor == 0 : Current.next.Contains(node);

    public static bool Choose(MapNode node)
    {
        if (!CanChoose(node)) return false;
        Current = node;
        return true;
    }

    // Staircase: each step moves down one floor or the other, so nothing crosses
    // and every node gets at least one link in and one out.
    static void Connect(List<MapNode> from, List<MapNode> to)
    {
        int i = 0, j = 0;
        from[i].next.Add(to[j]);

        while (i < from.Count - 1 || j < to.Count - 1)
        {
            bool canAdvanceFrom = i < from.Count - 1;
            bool canAdvanceTo = j < to.Count - 1;

            if (canAdvanceFrom && (!canAdvanceTo || Random.value < 0.5f)) i ++;
            else j++;

            from[i].next.Add(to[j]);
        }
    }

    static StageReward PickReward()
    {
        float roll = Random.value * (CardsWeight + MysteryCardWeight);
        return roll < CardsWeight ? StageReward.Cards : StageReward.MysteryCard;
    }

    // Exactly one heal node per map. It has no stage: it is resolved on the map itself
    static void PlaceHealNode()
    {
        var candidates = new List<MapNode>();
        for (int f = FirstHealFloor; f <= FloorCount - 2; f++)
            candidates.AddRange(floors[f]);

        if (candidates.Count == 0) return;

        MapNode heal = candidates[Random.Range(0, candidates.Count)];
        heal.reward = StageReward.Heal;
        heal.stage = null;
    }

    // ASCII drawing of the map for debugging, followed by one legend line per node.
    // The Unity console font is not monospaced: if it looks misaligned, paste it into a text editor.
    public static string Describe()
    {
        const int Slots = MaxNodesPerFloor;   // columns of the drawing
        const int CellWidth = 12;
        const int ConnectorRows = 3;
        int width = Slots * CellWidth;

        int CenterX(int slot) => slot * CellWidth + CellWidth / 2;
        int Slot(int index, int count) =>
            count == 1 ? (Slots - 1) / 2 : Mathf.RoundToInt(index * (Slots - 1f) / (count - 1));
        char Symbol(StageReward reward) => reward switch
        {
            StageReward.Cards => 'C',
            StageReward.MysteryCard => '?',
            StageReward.Heal => '+',
            _ => 'B'
        };

        var sb = new System.Text.StringBuilder("=== RUN MAP ===  (C cards, ? mystery card, + heal, B boss, * you are here)\n");
        var line = new char[width];

        for (int f = 0; f < floors.Count; f++)
        {
            System.Array.Fill(line, ' ');
            foreach (var node in floors[f])
            {
                string label = $"{(node == Current ? "*" : "")}[{Symbol(node.reward)} {node.floor}.{node.index}]";
                int start = CenterX(Slot(node.index, floors[f].Count)) - label.Length / 2;
                label.CopyTo(0, line, start, label.Length);
            }
            sb.AppendLine(new string(line).TrimEnd());

            if (f == floors.Count - 1) break;

            // links to the next floor, drawn over several rows
            for (int r = 0; r < ConnectorRows; r++)
            {
                System.Array.Fill(line, ' ');
                foreach (var node in floors[f])
                {
                    int from = CenterX(Slot(node.index, floors[f].Count));
                    foreach (var next in node.next)
                    {
                        int to = CenterX(Slot(next.index, floors[f + 1].Count));
                        int x = Mathf.RoundToInt(Mathf.Lerp(from, to, (r + 0.5f) / ConnectorRows));
                        char c = from == to ? '|' : (to > from ? '\\' : '/');
                        line[x] = (line[x] == ' ' || line[x] == c) ? c : 'X';
                    }
                }
                sb.AppendLine(new string(line).TrimEnd());
            }
        }

        sb.AppendLine("Nodes:");
        foreach (var floor in floors)
            foreach (var node in floor)
                sb.AppendLine($"  {node.floor}.{node.index}  {node.reward}{(node.stage != null ? "  " + node.stage.sceneName : "")}");

        return sb.ToString();
    }
}