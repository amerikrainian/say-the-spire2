using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("travel", "Travel", category: "Map")]
public class TravelEvent : GameEvent
{
    private readonly string _nodeName;

    public TravelEvent(string nodeName)
    {
        _nodeName = nodeName;
    }

    public override Message? GetMessage() =>
        Message.Localized("ui", "EVENT.MAP_TRAVEL", new { node = _nodeName });

    public override bool ShouldAddToBuffer() => false;
}
