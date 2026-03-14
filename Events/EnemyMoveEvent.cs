using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("enemy_move", "Enemy Moves", hasSourceFilter: true, allowCurrentPlayer: false, allowOtherPlayers: false)]
public class EnemyMoveEvent : GameEvent
{
    private readonly string _creatureName;

    public EnemyMoveEvent(Creature creature)
    {
        Source = creature;
        _creatureName = creature.Name;
    }

    public override string? GetMessage() => _creatureName;
}
