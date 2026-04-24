using System.Collections.Generic;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A shop item's price state: gold cost plus modifiers (on-sale flag,
/// insufficient-gold warning) when the item is still stocked, or a
/// "sold out" indicator when it isn't. Bundling these into one announcement
/// keeps the shop-context block contiguous in focus strings and lets the user
/// toggle the sold-out indicator via a price-specific "sold_out" setting.
/// </summary>
public sealed class PriceAnnouncement : Announcement
{
    private readonly int _cost;
    private readonly bool _canAfford;
    private readonly bool _isOnSale;
    private readonly bool _soldOut;

    public PriceAnnouncement(int cost, bool canAfford = true, bool isOnSale = false)
    {
        _cost = cost;
        _canAfford = canAfford;
        _isOnSale = isOnSale;
        _soldOut = false;
    }

    private PriceAnnouncement() { _soldOut = true; }

    public static PriceAnnouncement SoldOut() => new();

    public override string Key => "price";
    public override string Suffix => ",";

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("sold_out", "Sold Out", true,
            localizationKey: "SETTINGS.ANNOUNCEMENTS.SOLD_OUT"));
    }

    public override Message Render(AnnouncementContext ctx)
    {
        if (_soldOut)
        {
            return ctx.ResolveBool(Key, "sold_out", true)
                ? Message.Localized("ui", "LABELS.SOLD_OUT")
                : Message.Empty;
        }

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
