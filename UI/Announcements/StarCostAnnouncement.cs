using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A card's star cost. Handles both fixed costs and X costs, with a verbose
/// toggle that expands "3" to "3 stars".
/// </summary>
public sealed class StarCostAnnouncement : Announcement
{
    private readonly int _cost;
    private readonly bool _isX;
    private readonly bool _verbose;

    public StarCostAnnouncement(int cost, bool isX, bool verbose)
    {
        _cost = cost;
        _isX = isX;
        _verbose = verbose;
    }

    public override string Key => "star_cost";
    public override string Suffix => ",";

    public override Message Render()
    {
        if (_isX)
            return _verbose
                ? Message.Localized("ui", "RESOURCE.CARD_X_STARS")
                : Message.Raw("X");

        return _verbose
            ? Message.Localized("ui", "RESOURCE.CARD_STAR_COST", new { cost = _cost })
            : Message.Raw(_cost.ToString());
    }
}
