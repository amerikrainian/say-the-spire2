using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A player's current / max energy pool. Honors a "verbose" setting
/// (default true) that cascades per-element / global:
/// - Verbose: "3/3 energy"
/// - Compact: "3/3"
/// </summary>
public sealed class EnergyAnnouncement : Announcement
{
    private readonly int _current;
    private readonly int _max;

    public EnergyAnnouncement(int current, int max)
    {
        _current = current;
        _max = max;
    }

    public override string Key => "energy";
    public override string Suffix => ",";

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("verbose", "Verbose", true, localizationKey: "SETTINGS.VERBOSE"));
    }

    public override Message Render(AnnouncementContext ctx)
    {
        var verbose = ctx.ResolveBool(Key, "verbose", true);
        return verbose
            ? Message.Localized("ui", "RESOURCE.ENERGY", new { current = _current, max = _max })
            : Message.Localized("ui", "RESOURCE.ENERGY_COMPACT", new { current = _current, max = _max });
    }
}
