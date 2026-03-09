using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_upgrade", "Card Upgrade")]
public class CardUpgradeEvent : GameEvent
{
    private readonly string _cardName;

    public CardUpgradeEvent(string cardName)
    {
        _cardName = cardName;
    }

    public override string? GetMessage() => $"{_cardName} upgraded";
}
