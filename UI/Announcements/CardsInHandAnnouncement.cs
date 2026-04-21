using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>Number of cards currently in a player's hand.</summary>
public sealed class CardsInHandAnnouncement : Announcement
{
    private readonly int _count;

    public CardsInHandAnnouncement(int count) { _count = count; }

    public override string Key => "cards_in_hand";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "RESOURCE.CARDS_IN_HAND", new { count = _count });
}
