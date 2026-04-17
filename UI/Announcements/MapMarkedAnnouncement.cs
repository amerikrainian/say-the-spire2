using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>User has flagged this map node with a marker. Stateless — only yielded when marked.</summary>
public sealed class MapMarkedAnnouncement : Announcement
{
    public override string Key => "map_marked";
    public override string Suffix => ",";
    public override Message Render() => Message.Localized("map_nav", "MARKERS.MARKED");
}
