using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// In multiplayer, what a teammate's focus is currently on (card / relic /
/// potion / power) — their "intent" analogue to a monster's attack intent.
/// Takes a pre-composed summary produced by the caller (typically via
/// CreatureIntentFormatter) and wraps it with the shared CREATURE.INTENT_PREFIX.
/// </summary>
public sealed class PlayerIntentsAnnouncement : Announcement
{
    private readonly Message _summary;

    public PlayerIntentsAnnouncement(Message summary) { _summary = summary; }

    public override string Key => "player_intents";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx)
    {
        if (_summary.IsEmpty) return Message.Empty;
        return Message.Join(" ", Message.Localized("ui", "CREATURE.INTENT_PREFIX"), _summary);
    }
}
