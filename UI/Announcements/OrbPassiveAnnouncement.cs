using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>An orb's passive value.</summary>
public sealed class OrbPassiveAnnouncement : Announcement
{
    private readonly int _value;

    public OrbPassiveAnnouncement(int value) { _value = value; }

    public override string Key => "orb_passive";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "ORB.PASSIVE", new { value = _value });
}
