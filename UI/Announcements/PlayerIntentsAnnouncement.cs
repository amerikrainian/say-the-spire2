using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// In multiplayer, what a teammate's focus is currently on (card / relic /
/// potion / power) — their "intent" analogue to a monster's attack intent.
/// Takes a pre-formatted summary string produced by the caller (typically via
/// CreatureIntentFormatter) and wraps it with the shared CREATURE.INTENT_PREFIX.
/// </summary>
public sealed class PlayerIntentsAnnouncement : Announcement
{
    private readonly string _summary;

    public PlayerIntentsAnnouncement(string summary) { _summary = summary; }

    public override string Key => "player_intents";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx)
    {
        if (string.IsNullOrEmpty(_summary)) return Message.Empty;
        var prefix = LocalizationManager.GetOrDefault("ui", "CREATURE.INTENT_PREFIX", "Intent");
        return Message.Raw($"{prefix} {_summary}");
    }
}
