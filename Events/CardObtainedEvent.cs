using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_obtained", "Card Obtained")]
public class CardObtainedEvent : GameEvent
{
    private readonly string _cardName;

    public CardObtainedEvent(string cardName)
    {
        _cardName = cardName;
    }

    public override string? GetMessage() => $"{_cardName} obtained";
}
