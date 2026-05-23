using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A player's current gold total. Honors a "verbose" setting (default true)
/// that cascades per-element / per-buffer / per-hotkey / global:
/// - Verbose: "99 gold"
/// - Compact: "99"
/// </summary>
[ShowInGlobalSettings]
public sealed class GoldAnnouncement : Announcement
{
    private readonly int _amount;

    public GoldAnnouncement(int amount) { _amount = amount; }

    public override string Key => "gold";

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("verbose", "Verbose", true, localizationKey: "SETTINGS.VERBOSE"));
    }

    public override Message Render(AnnouncementContext ctx)
    {
        var verbose = ctx.ResolveBool(Key, "verbose", true);
        return verbose
            ? Message.Localized("ui", "RESOURCE.GOLD", new { amount = _amount })
            : Message.Localized("ui", "RESOURCE.GOLD_COMPACT", new { amount = _amount });
    }
}
