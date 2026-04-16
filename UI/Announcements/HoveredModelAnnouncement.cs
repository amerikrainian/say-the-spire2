using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// What a player is currently hovering over (card / relic / potion / power).
/// Takes a pre-formatted summary string produced by the caller (typically via
/// CreatureIntentFormatter) and wraps it with the shared CREATURE.INTENT_PREFIX.
/// </summary>
public sealed class HoveredModelAnnouncement : Announcement
{
    private readonly string _summary;

    public HoveredModelAnnouncement(string summary) { _summary = summary; }

    public override string Key => "hovered_model";
    public override string Suffix => ",";

    public override Message Render()
    {
        if (string.IsNullOrEmpty(_summary)) return Message.Empty;
        var prefix = LocalizationManager.GetOrDefault("ui", "CREATURE.INTENT_PREFIX", "Intent");
        return Message.Raw($"{prefix} {_summary}");
    }
}
