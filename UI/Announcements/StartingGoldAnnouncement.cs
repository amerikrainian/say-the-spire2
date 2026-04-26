using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>A character's starting gold value on the character-select screen.</summary>
public sealed class StartingGoldAnnouncement : Announcement
{
    private readonly int _amount;

    public StartingGoldAnnouncement(int amount) { _amount = amount; }

    public override string Key => "starting_gold";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "CHARACTER.STARTING_GOLD", new { amount = _amount });
}
