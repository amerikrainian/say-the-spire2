using System.Collections.Generic;

namespace SayTheSpire2.UI.Elements;

public class Container : UIElement
{
    private readonly List<UIElement> _children = new();

    public IReadOnlyList<UIElement> Children => _children;
    public string? ContainerLabel { get; set; }
    public bool AnnounceName { get; set; } = true;
    public bool AnnouncePosition { get; set; } = true;

    public override string? GetLabel() => ContainerLabel;

    public void Add(UIElement child)
    {
        _children.Add(child);
        child.Parent = this;
    }

    public void Remove(UIElement child)
    {
        if (_children.Remove(child))
            child.Parent = null;
    }

    public void Clear()
    {
        foreach (var child in _children)
            child.Parent = null;
        _children.Clear();
    }

    public int IndexOf(UIElement child) => _children.IndexOf(child);

    /// <summary>
    /// Returns 1-based position and total count for a child element.
    /// Returns null if the child is not in this container.
    /// </summary>
    public virtual (int index, int count)? GetChildPosition(UIElement child)
    {
        var idx = _children.IndexOf(child);
        if (idx < 0) return null;
        return (idx + 1, _children.Count);
    }
}
