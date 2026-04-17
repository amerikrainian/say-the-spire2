using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Lists the user-placed map markers reachable from this node along the travel
/// path. Renders only when at least one marker is on path.
/// </summary>
public sealed class OnPathAnnouncement : Announcement
{
    private readonly IReadOnlyList<string> _markers;

    public OnPathAnnouncement(IReadOnlyList<string> markers) { _markers = markers; }

    public override string Key => "on_path";
    public override string Suffix => ",";
    public override Message Render()
    {
        if (_markers.Count == 0) return Message.Empty;
        return Message.Localized("map_nav", "NAV.ON_PATH_TO", new
        {
            markers = string.Join(", ", _markers)
        });
    }
}
