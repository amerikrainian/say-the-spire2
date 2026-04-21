using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>A player's current / max energy.</summary>
public sealed class EnergyAnnouncement : Announcement
{
    private readonly int _current;
    private readonly int _max;

    public EnergyAnnouncement(int current, int max)
    {
        _current = current;
        _max = max;
    }

    public override string Key => "energy";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "RESOURCE.ENERGY", new { current = _current, max = _max });
}
