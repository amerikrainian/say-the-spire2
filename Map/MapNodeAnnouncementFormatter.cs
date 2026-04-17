using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;

namespace SayTheSpire2.Map;

public static class MapNodeAnnouncementFormatter
{
    /// <summary>
    /// Structured snapshot of everything announceable about a map node.
    /// Focus-string builders convert this into individual Announcement instances;
    /// one-shot callers render it to a single string via DescribeNode.
    /// </summary>
    public static MapNodeView BuildView(MapNode node, MapHandler handler, IReadOnlyList<MapNode>? rowNodes = null,
        MapNode? travelOrigin = null,
        MapReachabilityContext? travelContext = null, MapReachabilityContext? nodeContext = null)
    {
        rowNodes ??= GetDefaultRowNodes(node);
        var effectiveOrigin = travelOrigin ?? GetDefaultTravelOrigin(node);
        var effectiveTravelContext = travelContext ?? ResolveContextAtNode(handler, effectiveOrigin);
        var effectiveNodeContext = nodeContext ?? ResolveContextAtNode(handler, node);

        var isMarked = MapMarkerState.IsMarked(node.Point);
        var isFreeTravel = effectiveOrigin != null &&
            MapReachability.IsFreeTravelOnly(effectiveOrigin, node, handler, effectiveTravelContext);

        var (onPath, diverges) = BuildMarkerLabels(node, handler, rowNodes, effectiveOrigin,
            effectiveTravelContext, effectiveNodeContext);
        var voters = BuildVotersList(node);

        return new MapNodeView(
            TypeName: node.GetDisplayName(),
            Coordinates: node.GetCoordinatesString(),
            State: node.GetStateString(),
            IsMarked: isMarked,
            IsFreeTravel: isFreeTravel,
            OnPathMarkers: onPath,
            DivergingMarkers: diverges,
            Voters: voters,
            IsChoice: rowNodes.Count > 1);
    }

    /// <summary>
    /// Renders a node description as a single string. Used by non-focus callers
    /// (TreeMapViewer, MapScreen.DescribePoint) that want a one-shot speech line.
    /// Focus-string usage goes through the announcement pipeline instead.
    /// </summary>
    public static string DescribeNode(MapNode node, MapHandler handler, IReadOnlyList<MapNode>? rowNodes = null,
        bool includeChoicePrefix = false, MapNode? travelOrigin = null,
        MapReachabilityContext? travelContext = null, MapReachabilityContext? nodeContext = null)
    {
        var view = BuildView(node, handler, rowNodes, travelOrigin, travelContext, nodeContext);
        return RenderView(view, includeChoicePrefix);
    }

    private static string RenderView(MapNodeView view, bool includeChoicePrefix)
    {
        var type = view.TypeName;
        if (view.IsMarked)
            type = $"{GetMarkedText()}, {type}";
        if (view.IsFreeTravel)
            type = $"{type}, {GetFreeTravelText()}";

        var announcement = view.State != null
            ? Message.Localized("map_nav", "NAV.NODE_WITH_STATE", new
            {
                type,
                coordinates = view.Coordinates,
                state = view.State
            }).Resolve()
            : Message.Localized("map_nav", "NAV.NODE", new
            {
                type,
                coordinates = view.Coordinates
            }).Resolve();

        var extras = new List<string>();
        if (view.OnPathMarkers.Count > 0)
            extras.Add(Message.Localized("map_nav", "NAV.ON_PATH_TO", new
            {
                markers = string.Join(", ", view.OnPathMarkers)
            }).Resolve());
        if (view.DivergingMarkers.Count > 0)
            extras.Add(Message.Localized("map_nav", "NAV.DIVERGES_FROM", new
            {
                markers = string.Join(", ", view.DivergingMarkers)
            }).Resolve());
        if (view.Voters.Count > 0)
            extras.Add(Message.Localized("ui", "EVENT.VOTED_FOR_BY", new
            {
                players = string.Join(", ", view.Voters)
            }).Resolve());

        if (extras.Count > 0)
            announcement = $"{announcement}, {string.Join(", ", extras)}";

        if (includeChoicePrefix && view.IsChoice)
            announcement = $"{GetChoiceText()}, {announcement}";

        return announcement;
    }

    public static List<MapNode> GetDefaultRowNodes(MapNode node)
    {
        MapNode? parent = node.BackwardEdges
            .FirstOrDefault(edge => edge.From.State == MegaCrit.Sts2.Core.Map.MapPointState.Traveled)
            ?.From
            ?? node.BackwardEdges.FirstOrDefault()?.From;

        if (parent == null)
            return new List<MapNode> { node };

        return parent.ForwardEdges
            .Select(edge => edge.To)
            .OrderBy(child => child.Col)
            .ToList();
    }

    private static (List<string> onPath, List<string> diverges) BuildMarkerLabels(MapNode node, MapHandler handler,
        IReadOnlyList<MapNode> rowNodes, MapNode? travelOrigin,
        MapReachabilityContext travelContext, MapReachabilityContext nodeContext)
    {
        var markedCoords = new HashSet<MegaCrit.Sts2.Core.Map.MapCoord>(MapMarkerState.GetMarkedCoords());
        if (markedCoords.Count == 0)
            return (new List<string>(), new List<string>());

        var currentReachable = MapReachability.GetReachableMarkedCoords(node, handler, nodeContext, markedCoords);
        currentReachable.Remove(node.Point.coord);

        var alternativeReachable = new HashSet<MegaCrit.Sts2.Core.Map.MapCoord>();
        if (rowNodes.Count > 1 && travelOrigin != null)
        {
            foreach (var sibling in rowNodes)
            {
                if (sibling.Point.coord.Equals(node.Point.coord))
                    continue;

                if (!MapReachability.TryAdvance(travelOrigin, sibling, handler, travelContext, out var siblingContext))
                    continue;

                alternativeReachable.UnionWith(MapReachability.GetReachableMarkedCoords(sibling, handler, siblingContext, markedCoords));
            }
        }

        var onPathNodes = SortByVisualOrder(currentReachable
            .Select(handler.GetNode)
            .OfType<MapNode>());
        var divergesNodes = SortByVisualOrder(alternativeReachable
            .Where(coord => !currentReachable.Contains(coord))
            .Select(handler.GetNode)
            .OfType<MapNode>());

        var duplicateNames = GetDuplicateNames(onPathNodes.Concat(divergesNodes));
        return (
            onPathNodes.Select(marker => GetMarkerLabel(marker, duplicateNames)).ToList(),
            divergesNodes.Select(marker => GetMarkerLabel(marker, duplicateNames)).ToList()
        );
    }

    private static List<string> BuildVotersList(MapNode node)
    {
        if (!MultiplayerHelper.IsMultiplayer())
            return new List<string>();

        IReadOnlyList<MegaCrit.Sts2.Core.Entities.Players.Player>? players;
        try
        {
            players = RunManager.Instance.DebugOnlyGetState()?.Players;
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Failed to read map vote players: {e.Message}");
            return new List<string>();
        }

        if (players == null || players.Count == 0)
            return new List<string>();

        var voters = new List<string>();
        foreach (var player in players)
        {
            try
            {
                var vote = RunManager.Instance.MapSelectionSynchronizer.GetVote(player);
                if (vote?.coord.Equals(node.Point.coord) == true)
                    voters.Add(MultiplayerHelper.GetPlayerName(player));
            }
            catch (System.Exception e)
            {
                Log.Info($"[AccessibilityMod] Failed to read map vote for {player.NetId}: {e.Message}");
            }
        }
        return voters;
    }

    private static MapNode? GetDefaultTravelOrigin(MapNode node)
    {
        return node.BackwardEdges
            .FirstOrDefault(edge => edge.From.State == MegaCrit.Sts2.Core.Map.MapPointState.Traveled)
            ?.From
            ?? node.BackwardEdges.FirstOrDefault()?.From;
    }

    private static MapReachabilityContext ResolveContextAtNode(MapHandler handler, MapNode? node)
    {
        if (node == null)
            return handler.ReachabilityContext;

        var start = handler.CurrentNode;
        if (start == null)
            return handler.ReachabilityContext;

        return MapReachability.TryGetBestContextAtNode(start, handler, handler.ReachabilityContext, node, out var context)
            ? context
            : handler.ReachabilityContext;
    }

    private static List<MapNode> SortByVisualOrder(IEnumerable<MapNode> nodes)
    {
        return nodes.OrderBy(node => node.Row)
            .ThenBy(node => node.Col)
            .ToList();
    }

    private static HashSet<string> GetDuplicateNames(IEnumerable<MapNode> nodes)
    {
        return nodes.GroupBy(node => node.GetDisplayName())
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();
    }

    private static string GetMarkerLabel(MapNode node, HashSet<string> duplicateNames)
    {
        var name = node.GetDisplayName();
        return duplicateNames.Contains(name)
            ? $"{name} {node.GetCoordinatesString()}"
            : name;
    }

    private static string GetChoiceText()
    {
        return LocalizationManager.GetOrDefault("map_nav", "NAV.CHOICE", "choice");
    }

    private static string GetMarkedText()
    {
        return LocalizationManager.GetOrDefault("map_nav", "MARKERS.MARKED", "Marked");
    }

    private static string GetFreeTravelText()
    {
        return LocalizationManager.GetOrDefault("map_nav", "NAV.FREE_TRAVEL", "fly");
    }
}
