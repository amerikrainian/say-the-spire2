using System.Collections.Generic;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A card's cost to play — covers both the energy and star components,
/// since they always appear together in the focus string and the user
/// configures them as one unit. Each sub-cost accepts a fixed value or an
/// X-cost flag. Honors a "verbose" setting (default true) that cascades
/// per-element / global:
/// - Verbose: "1 energy, 3 stars" / "X energy, 2 stars"
/// - Compact: "1, 3" / "X, 2"
/// </summary>
public sealed class EnergyCostAnnouncement : Announcement
{
    private readonly int? _energyCost;
    private readonly bool _energyIsX;
    private readonly int? _starCost;
    private readonly bool _starIsX;

    public EnergyCostAnnouncement(
        int? energyCost = null, bool energyIsX = false,
        int? starCost = null, bool starIsX = false)
    {
        _energyCost = energyCost;
        _energyIsX = energyIsX;
        _starCost = starCost;
        _starIsX = starIsX;
    }

    public override string Key => "energy_cost";
    public override string Suffix => ",";

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("verbose", "Verbose", true, localizationKey: "SETTINGS.VERBOSE"));
    }

    public override Message Render(AnnouncementContext ctx)
    {
        var verbose = ctx.ResolveBool(Key, "verbose", true);
        var parts = new List<Message>();

        if (_energyCost.HasValue)
        {
            if (_energyIsX)
                parts.Add(verbose ? Message.Localized("ui", "RESOURCE.CARD_X_ENERGY") : Message.Raw("X"));
            else
                parts.Add(verbose
                    ? Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost = _energyCost.Value })
                    : Message.Raw(_energyCost.Value.ToString()));
        }

        if (_starCost.HasValue)
        {
            if (_starIsX)
                parts.Add(verbose ? Message.Localized("ui", "RESOURCE.CARD_X_STARS") : Message.Raw("X"));
            else
                parts.Add(verbose
                    ? Message.Localized("ui", "RESOURCE.CARD_STAR_COST", new { cost = _starCost.Value })
                    : Message.Raw(_starCost.Value.ToString()));
        }

        return parts.Count == 0 ? Message.Empty : Message.Join(", ", parts.ToArray());
    }
}
