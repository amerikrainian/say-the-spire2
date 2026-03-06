namespace SayTheSpire2.Map;

public class MapEdge
{
    public MapNode From { get; }
    public MapNode To { get; }

    public MapEdge(MapNode from, MapNode to)
    {
        From = from;
        To = to;
    }
}
