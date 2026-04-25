using SayTheSpire2.Localization;

namespace SayTheSpire2.Map;

public abstract class MapViewer
{
    protected MapHandler Handler { get; }
    protected MapNode? Current { get; set; }
    public MapNode? CurrentNode => Current;

    protected MapViewer(MapHandler handler)
    {
        Handler = handler;
    }

    public abstract void SetStartNode(MapNode focusedNode);

    /// <summary>
    /// Move forward (toward boss). Returns announcement text.
    /// </summary>
    public abstract Message? MoveForward();

    /// <summary>
    /// Move backward (toward start). Returns announcement text.
    /// </summary>
    public abstract Message? MoveBackward();

    /// <summary>
    /// Cycle to the next branch at a choice point. Returns announcement text.
    /// </summary>
    public abstract Message? NextBranch();

    /// <summary>
    /// Cycle to the previous branch at a choice point. Returns announcement text.
    /// </summary>
    public abstract Message? PreviousBranch();

    /// <summary>
    /// Move the viewer cursor directly onto a specific node and return the
    /// standard node announcement for that position.
    /// </summary>
    public abstract Message? JumpToNode(MapNode node);
}
