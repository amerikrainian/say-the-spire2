using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>A player's current star count (voidless-character resource).</summary>
public sealed class StarsAnnouncement : Announcement
{
    private readonly int _amount;

    public StarsAnnouncement(int amount) { _amount = amount; }

    public override string Key => "stars";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "RESOURCE.STARS", new { amount = _amount });
}
