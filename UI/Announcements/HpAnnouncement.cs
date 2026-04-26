using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>A creature or player's current / max HP.</summary>
public sealed class HpAnnouncement : Announcement
{
    private readonly int _current;
    private readonly int _max;

    public HpAnnouncement(int current, int max)
    {
        _current = current;
        _max = max;
    }

    public override string Key => "hp";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "RESOURCE.HP", new { current = _current, max = _max });
}
