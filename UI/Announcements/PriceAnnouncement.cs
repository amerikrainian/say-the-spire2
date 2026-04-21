using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A shop item's gold price plus modifiers (on-sale flag, insufficient-gold warning).
/// Bundles what used to be three separate announcements so focus-string users hear
/// "30 gold, on sale, not enough gold" as one contiguous block instead of fighting
/// to keep them adjacent.
/// </summary>
public sealed class PriceAnnouncement : Announcement
{
    private readonly int _cost;
    private readonly bool _canAfford;
    private readonly bool _isOnSale;

    public PriceAnnouncement(int cost, bool canAfford = true, bool isOnSale = false)
    {
        _cost = cost;
        _canAfford = canAfford;
        _isOnSale = isOnSale;
    }

    public override string Key => "price";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx)
    {
        var parts = new List<Message>
        {
            Message.Localized("ui", "RESOURCE.PRICE", new { cost = _cost })
        };
        if (_isOnSale)
            parts.Add(Message.Localized("ui", "RESOURCE.ON_SALE"));
        if (!_canAfford)
            parts.Add(Message.Localized("ui", "RESOURCE.NOT_ENOUGH_GOLD"));
        return Message.Join(", ", parts.ToArray());
    }
}
