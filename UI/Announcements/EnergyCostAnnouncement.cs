using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A card's energy cost. Handles both fixed costs (e.g. "2 energy") and X costs
/// (costs all available energy), with a verbose toggle that expands "2" to "2 energy".
/// </summary>
public sealed class EnergyCostAnnouncement : Announcement
{
    private readonly int _cost;
    private readonly bool _isX;
    private readonly bool _verbose;

    public EnergyCostAnnouncement(int cost, bool isX, bool verbose)
    {
        _cost = cost;
        _isX = isX;
        _verbose = verbose;
    }

    public override string Key => "energy_cost";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx)
    {
        if (_isX)
            return _verbose
                ? Message.Localized("ui", "RESOURCE.CARD_X_ENERGY")
                : Message.Raw("X");

        return _verbose
            ? Message.Localized("ui", "RESOURCE.CARD_ENERGY_COST", new { cost = _cost })
            : Message.Raw(_cost.ToString());
    }
}
