using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Lists markers that are NOT reachable from this node but would be reachable
/// via a sibling in the same row — i.e., choosing this node means giving up
/// access to those markers. Renders only when there's at least one.
/// </summary>
public sealed class DivergesAnnouncement : Announcement
{
    private readonly IReadOnlyList<string> _markers;

    public DivergesAnnouncement(IReadOnlyList<string> markers) { _markers = markers; }

    public override string Key => "diverges";
    public override string Suffix => ",";
    public override Message Render()
    {
        if (_markers.Count == 0) return Message.Empty;
        return Message.Localized("map_nav", "NAV.DIVERGES_FROM", new
        {
            markers = string.Join(", ", _markers)
        });
    }
}
