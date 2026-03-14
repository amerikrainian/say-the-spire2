using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("card_obtained", "Card Obtained", hasSourceFilter: true, allowEnemies: false)]
public class CardObtainedEvent : GameEvent
{
    private readonly string _cardName;

    public CardObtainedEvent(string cardName, Creature? source = null)
    {
        Source = source;
        _cardName = cardName;
    }

    public override string? GetMessage() => $"{_cardName} obtained";
}
