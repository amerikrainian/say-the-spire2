using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A creature's current block amount. Honors a "verbose" setting (default
/// true) that cascades per-element / per-buffer / per-hotkey / global:
/// - Verbose: "5 block"
/// - Compact: "5"
/// </summary>
[ShowInGlobalSettings]
public sealed class BlockAnnouncement : Announcement
{
    private readonly int _amount;

    public BlockAnnouncement(int amount) { _amount = amount; }

    public override string Key => "block";
    public override string Suffix => ",";

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("verbose", "Verbose", true, localizationKey: "SETTINGS.VERBOSE"));
    }

    public override Message Render(AnnouncementContext ctx)
    {
        var verbose = ctx.ResolveBool(Key, "verbose", true);
        return verbose
            ? Message.Localized("ui", "RESOURCE.BLOCK", new { amount = _amount })
            : Message.Localized("ui", "RESOURCE.BLOCK_COMPACT", new { amount = _amount });
    }
}
