using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaCrit.Sts2.Core.Map;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Map;

public class TreeMapViewer : MapViewer
{
    private readonly Stack<MapEdge> _pathStack = new();
    private bool _autoAdvance;

    // Cached row nodes for left/right navigation
    private List<MapNode> _rowNodes = new();
    private int _rowIndex;

    public TreeMapViewer(MapHandler handler, bool autoAdvance = false) : base(handler)
    {
        _autoAdvance = autoAdvance;
    }

    public override void SetStartNode(MapNode focusedNode)
    {
        _pathStack.Clear();
        Current = focusedNode;
        RefreshSiblings();
    }

    public override string? MoveForward()
    {
        if (Current == null) return null;

        var children = Current.ForwardEdges;
        if (children.Count == 0)
            return LocalizationManager.Get("map_nav", "NAV.NO_FORWARD");

        // Always move to leftmost child by column
        var edge = children.OrderBy(e => e.To.Col).First();
        _pathStack.Push(edge);
        Current = edge.To;
        RefreshSiblings();

        string announcement;
        if (_autoAdvance)
            announcement = AutoAdvanceForward();
        else
            announcement = AnnounceCurrentNode();

        if (children.Count > 1)
            announcement = GetChoiceText() + ", " + announcement;
        return announcement;
    }

    public override string? MoveBackward()
    {
        if (Current == null) return null;

        if (_pathStack.Count > 0)
        {
            var edge = _pathStack.Pop();
            Current = edge.From;
            RefreshSiblings();

            if (_autoAdvance)
                return AutoAdvanceBackward();

            return AnnounceCurrentNode();
        }

        // No path stack — try to go to a parent
        var parents = Current.BackwardEdges;
        if (parents.Count == 0)
            return LocalizationManager.Get("map_nav", "NAV.NO_BACKWARD");

        // Prefer traveled parent
        var parent = parents.FirstOrDefault(e => e.From.State == MapPointState.Traveled)
                     ?? parents[0];

        Current = parent.From;
        RefreshSiblings();

        if (_autoAdvance)
            return AutoAdvanceBackward();

        return AnnounceCurrentNode();
    }

    public override string? NextBranch()
    {
        if (Current == null) return null;
        if (_rowNodes.Count <= 1 || _rowIndex >= _rowNodes.Count - 1)
            return null;

        _rowIndex++;
        Current = _rowNodes[_rowIndex];
        return AnnounceCurrentNode();
    }

    public override string? PreviousBranch()
    {
        if (Current == null) return null;
        if (_rowNodes.Count <= 1 || _rowIndex <= 0)
            return null;

        _rowIndex--;
        Current = _rowNodes[_rowIndex];
        return AnnounceCurrentNode();
    }

    private void RefreshSiblings()
    {
        if (Current == null) return;

        // Determine which parent we came from
        MapNode? parent = null;
        if (_pathStack.Count > 0)
        {
            // We got here via a forward move — the parent is the edge's From
            parent = _pathStack.Peek().From;
        }
        else
        {
            // Initial focus or after exhausting the stack — pick a parent
            var parentEdge = Current.BackwardEdges
                .FirstOrDefault(e => e.From.State == MapPointState.Traveled)
                ?? Current.BackwardEdges.FirstOrDefault();
            parent = parentEdge?.From;
        }

        if (parent != null)
        {
            // Siblings = children of that parent, sorted by column
            _rowNodes = parent.ForwardEdges
                .Select(e => e.To)
                .OrderBy(n => n.Col)
                .ToList();
        }
        else
        {
            // No parent (e.g., ancient node) — just this node
            _rowNodes = new List<MapNode> { Current };
        }

        _rowIndex = _rowNodes.IndexOf(Current);
        if (_rowIndex < 0) _rowIndex = 0;
    }

    private string AnnounceCurrentNode()
    {
        return AnnounceNode(Current!);
    }

    private string AutoAdvanceForward()
    {
        var sb = new StringBuilder();
        var visited = new List<MapNode> { Current! };

        while (Current!.ForwardEdges.Count == 1)
        {
            var edge = Current.ForwardEdges[0];
            _pathStack.Push(edge);
            Current = edge.To;
            visited.Add(Current);
        }

        RefreshSiblings();

        foreach (var node in visited)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(node.GetDisplayName());
        }

        return sb.ToString();
    }

    private string AutoAdvanceBackward()
    {
        var sb = new StringBuilder();
        var visited = new List<MapNode> { Current! };

        while (Current!.BackwardEdges.Count == 1 && _pathStack.Count > 0)
        {
            var edge = _pathStack.Pop();
            if (edge.From != Current.BackwardEdges[0].From)
            {
                _pathStack.Push(edge);
                break;
            }
            Current = edge.From;
            visited.Add(Current);

            if (Current.ForwardEdges.Count > 1)
                break;
        }

        RefreshSiblings();

        foreach (var node in visited)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(node.GetDisplayName());
        }

        return sb.ToString();
    }

    private static string AnnounceNode(MapNode node)
    {
        var state = node.GetStateString();
        if (state != null)
        {
            return new LocalizationString("map_nav", "NAV.NODE_WITH_STATE")
                .Add("type", node.GetDisplayName())
                .Add("coordinates", node.GetCoordinatesString())
                .Add("state", state)
                .ToString();
        }

        return new LocalizationString("map_nav", "NAV.NODE")
            .Add("type", node.GetDisplayName())
            .Add("coordinates", node.GetCoordinatesString())
            .ToString();
    }

    private static string GetChoiceText()
    {
        return LocalizationManager.Get("map_nav", "NAV.CHOICE") ?? "choice";
    }
}
