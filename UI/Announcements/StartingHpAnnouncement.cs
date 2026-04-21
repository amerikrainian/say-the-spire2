using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>A character's starting HP value on the character-select screen.</summary>
public sealed class StartingHpAnnouncement : Announcement
{
    private readonly int _amount;

    public StartingHpAnnouncement(int amount) { _amount = amount; }

    public override string Key => "starting_hp";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "CHARACTER.STARTING_HP", new { amount = _amount });
}
