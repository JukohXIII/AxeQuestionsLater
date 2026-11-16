using System.Collections.Generic;

public class MapNode
{
    public readonly int floor;
    public readonly int index;
    public StageReward reward;
    public StageDefinition stage;
    public readonly List<MapNode> next = new();

    public MapNode(int floor, int index)
    {
        this.floor = floor;
        this.index = index;
    }
}