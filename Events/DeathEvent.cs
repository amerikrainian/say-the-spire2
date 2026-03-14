using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("death", "Death", hasSourceFilter: true)]
public class DeathEvent : GameEvent
{
    private readonly string _creatureName;

    public DeathEvent(Creature creature)
    {
        Source = creature;
        _creatureName = creature.Name;
    }

    public override string? GetMessage() => $"{_creatureName} died";
}
