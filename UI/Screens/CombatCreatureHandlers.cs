using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Events;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Handles creature events (HP, block, power, death) for combat.
/// Extracted from CombatScreen for clarity.
/// </summary>
internal class CombatCreatureHandlers
{
    private readonly Creature _creature;

    public CombatCreatureHandlers(Creature creature)
    {
        _creature = creature;
    }

    public void OnBlockChanged(int oldBlock, int newBlock)
    {
        Log.Info($"[EventDebug] CreatureHandler.BlockChanged: {_creature.Name} {oldBlock}->{newBlock} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new BlockEvent(_creature, oldBlock, newBlock));
    }

    public void OnCurrentHpChanged(int oldHp, int newHp)
    {
        Log.Info($"[EventDebug] CreatureHandler.HpChanged: {_creature.Name} {oldHp}->{newHp} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new HpEvent(_creature, oldHp, newHp));
    }

    public void OnPowerIncreased(PowerModel power, int change, bool silent)
    {
        Log.Info($"[EventDebug] CreatureHandler.PowerIncreased: {_creature.Name} {power.Title.GetFormattedText()} +{change} silent={silent} handler={GetHashCode()}");
        if (!silent) EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Increased, change));
    }

    public void OnPowerDecreased(PowerModel power, bool silent)
    {
        Log.Info($"[EventDebug] CreatureHandler.PowerDecreased: {_creature.Name} {power.Title.GetFormattedText()} amount={power.Amount} silent={silent} handler={GetHashCode()}");
        // Skip if amount hit 0 — the PowerRemoved event will fire next and handle it.
        // Allow negative amounts (non-stacking powers use -1).
        if (!silent && power.Amount != 0)
            EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Decreased));
    }

    public void OnPowerRemoved(PowerModel power)
    {
        Log.Info($"[EventDebug] CreatureHandler.PowerRemoved: {_creature.Name} {power.Title.GetFormattedText()} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Removed));
    }

    public void OnDied(Creature c)
    {
        Log.Info($"[EventDebug] CreatureHandler.Died: {c.Name} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new DeathEvent(c));
    }
}
