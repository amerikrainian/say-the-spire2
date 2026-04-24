using System.Collections.Generic;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI;

/// <summary>
/// Tracks the container path to the last focused element and diffs against
/// the new path to determine which containers need to be announced.
/// </summary>
public class FocusContext
{
    private List<Container> _lastPath = new();

    /// <summary>
    /// Build the full announcement for an element, only including
    /// container context that changed since the last focus.
    /// </summary>
    public Message? BuildAnnouncement(UIElement element)
    {
        var newPath = BuildPath(element);
        var divergeIndex = FindDivergenceIndex(_lastPath, newPath);
        _lastPath = newPath;

        var parts = new List<Message>();

        // Announce containers that changed (from divergence point down)
        for (int i = divergeIndex; i < newPath.Count; i++)
        {
            var container = newPath[i];
            if (container.AnnounceName && !string.IsNullOrEmpty(container.ContainerLabel))
                parts.Add(Message.Raw(container.ContainerLabel));
        }

        // Append the element's own focus message (includes position, injected
        // via GetFocusMessage when Parent.AnnouncePosition is true).
        var focusMessage = element.GetFocusMessage();
        if (focusMessage != null && !focusMessage.IsEmpty)
            parts.Add(focusMessage);

        if (parts.Count == 0)
            return null;

        return Message.Join(", ", parts.ToArray());
    }

    /// <summary>
    /// Reset the tracked path (e.g., when leaving a screen).
    /// </summary>
    public void Reset()
    {
        _lastPath.Clear();
    }

    private static List<Container> BuildPath(UIElement element)
    {
        var path = new List<Container>();
        var current = element.Parent;
        while (current != null)
        {
            path.Add(current);
            current = current.Parent;
        }
        path.Reverse(); // root first
        return path;
    }

    private static int FindDivergenceIndex(List<Container> oldPath, List<Container> newPath)
    {
        int minLen = System.Math.Min(oldPath.Count, newPath.Count);
        for (int i = 0; i < minLen; i++)
        {
            if (!ReferenceEquals(oldPath[i], newPath[i]))
                return i;
        }
        if (newPath.Count > oldPath.Count)
            return oldPath.Count;
        return newPath.Count;
    }
}
