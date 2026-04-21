using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>An orb's evoke value (what triggers when the orb is consumed).</summary>
public sealed class OrbEvokeAnnouncement : Announcement
{
    private readonly int _value;

    public OrbEvokeAnnouncement(int value) { _value = value; }

    public override string Key => "orb_evoke";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "ORB.EVOKE", new { value = _value });
}
