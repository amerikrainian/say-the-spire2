using System.Collections.Generic;
using System.Linq;
using SayTheSpire2.Localization;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A monster's queued intents for its next move. Takes a structured list of
/// IntentViews (name + optional label) and renders them with the shared
/// CREATURE.INTENT_PREFIX. Empty when the monster has no queued intents.
/// </summary>
public sealed class MonsterIntentsAnnouncement : Announcement
{
    private readonly IReadOnlyList<IntentView> _intents;

    public MonsterIntentsAnnouncement(IReadOnlyList<IntentView> intents)
    {
        _intents = intents;
    }

    public override string Key => "monster_intents";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx)
    {
        if (_intents.Count == 0) return Message.Empty;

        var summaries = _intents.Select(i =>
            !string.IsNullOrEmpty(i.Label) ? $"{i.Name} {i.Label}" : i.Name);
        var joined = string.Join(", ", summaries);
        var prefix = LocalizationManager.GetOrDefault("ui", "CREATURE.INTENT_PREFIX", "Intent");
        return Message.Raw($"{prefix} {joined}");
    }
}
