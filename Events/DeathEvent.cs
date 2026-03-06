using MegaCrit.Sts2.Core.Entities.Creatures;

namespace SayTheSpire2.Events;

public class DeathEvent : GameEvent
{
    private readonly string _creatureName;

    public DeathEvent(Creature creature)
    {
        _creatureName = creature.Name;
    }

    public override string? GetMessage() => $"{_creatureName} died";
}
