using System.Collections.Generic;
using MegaCrit.Sts2.Core.Map;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Map;

public class MapNode
{
    public MapPoint Point { get; }
    public MapPointType PointType => Point.PointType;
    public MapPointState State { get; set; }
    public int Col => Point.coord.col;
    public int Row => Point.coord.row;

    public List<MapEdge> ForwardEdges { get; } = new();
    public List<MapEdge> BackwardEdges { get; } = new();

    public IEnumerable<MapNode> Children
    {
        get
        {
            foreach (var edge in ForwardEdges)
                yield return edge.To;
        }
    }

    public IEnumerable<MapNode> Parents
    {
        get
        {
            foreach (var edge in BackwardEdges)
                yield return edge.From;
        }
    }

    public MapNode(MapPoint point, MapPointState state)
    {
        Point = point;
        State = state;
    }

    /// <summary>
    /// Get the localized display name for a map point type.
    /// Shared by MapNode, ProxyMapPoint, and VotingHooks.
    /// </summary>
    public static string GetPointTypeName(MapPointType pointType)
    {
        var typeKey = pointType switch
        {
            MapPointType.Unknown => "NODE_TYPES.UNKNOWN",
            MapPointType.Shop => "NODE_TYPES.SHOP",
            MapPointType.Treasure => "NODE_TYPES.TREASURE",
            MapPointType.RestSite => "NODE_TYPES.REST_SITE",
            MapPointType.Monster => "NODE_TYPES.MONSTER",
            MapPointType.Elite => "NODE_TYPES.ELITE",
            MapPointType.Boss => "NODE_TYPES.BOSS",
            MapPointType.Ancient => "NODE_TYPES.ANCIENT",
            _ => "NODE_TYPES.UNKNOWN",
        };
        return LocalizationManager.GetOrDefault("map_nav", typeKey, pointType.ToString());
    }

    /// <summary>
    /// Get the localized display name for a MapPoint, including quest prefix.
    /// </summary>
    public static string GetPointDisplayName(MapPoint point)
    {
        var name = GetPointTypeName(point.PointType);
        if (point.Quests.Count > 0)
        {
            var questLabel = LocalizationManager.Get("map_nav", "QUEST_MARKED") ?? "Quest";
            name = questLabel + " " + name;
        }
        return name;
    }

    public virtual string GetDisplayName()
    {
        return GetPointDisplayName(Point);
    }

    public string? GetStateString()
    {
        if (State == MapPointState.Traveled)
            return LocalizationManager.Get("map_nav", "STATE.TRAVELED");
        return null;
    }

    public Message GetCoordinates() => GetCoordinates(Point);

    /// <summary>
    /// Get the localized coordinate message for a MapPoint (0-based).
    /// Shared by MapNode, ProxyMapPoint, and VotingHooks.
    /// </summary>
    public static Message GetCoordinates(MapPoint point) =>
        Message.Localized("map_nav", "NAV.COORDINATES", new { col = point.coord.col, row = point.coord.row });
}
