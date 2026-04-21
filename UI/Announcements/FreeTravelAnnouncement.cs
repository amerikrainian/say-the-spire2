using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// This map node is reachable from the current travel origin without costing a
/// turn / skipping intermediate rooms. Stateless — only yielded when free travel applies.
/// </summary>
public sealed class FreeTravelAnnouncement : Announcement
{
    public override string Key => "free_travel";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) => Message.Localized("map_nav", "NAV.FREE_TRAVEL");
}
