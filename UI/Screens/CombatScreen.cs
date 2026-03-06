using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Events;

namespace SayTheSpire2.UI.Screens;

public class CombatScreen : Screen
{
    private readonly CombatState _initialState;
    private readonly Dictionary<Creature, CreatureHandlers> _subscribedCreatures = new();

    public CombatScreen(CombatState state)
    {
        _initialState = state;
    }

    public override void OnPush()
    {
        SubscribeToAllCreatures(_initialState);
        CombatManager.Instance.CreaturesChanged += OnCreaturesChanged;
        CombatManager.Instance.TurnStarted += OnTurnStarted;
        Log.Info("[AccessibilityMod] CombatScreen pushed.");
    }

    public override void OnPop()
    {
        CombatManager.Instance.CreaturesChanged -= OnCreaturesChanged;
        CombatManager.Instance.TurnStarted -= OnTurnStarted;
        UnsubscribeAll();
        Log.Info("[AccessibilityMod] CombatScreen popped.");
    }

    private void OnCreaturesChanged(CombatState state)
    {
        SubscribeToAllCreatures(state);
    }

    private void OnTurnStarted(CombatState state)
    {
        EventDispatcher.Enqueue(new TurnEvent(state.CurrentSide, state.RoundNumber, isStart: true));
    }

    private void SubscribeToAllCreatures(CombatState state)
    {
        foreach (var creature in state.Creatures)
        {
            if (_subscribedCreatures.ContainsKey(creature)) continue;
            SubscribeToCreature(creature);
        }
    }

    private void SubscribeToCreature(Creature creature)
    {
        var handlers = new CreatureHandlers(creature);
        _subscribedCreatures[creature] = handlers;

        creature.BlockChanged += handlers.OnBlockChanged;
        creature.PowerIncreased += handlers.OnPowerIncreased;
        creature.PowerDecreased += handlers.OnPowerDecreased;
        creature.PowerRemoved += handlers.OnPowerRemoved;
        creature.Died += handlers.OnDied;
    }

    private void UnsubscribeAll()
    {
        foreach (var (creature, handlers) in _subscribedCreatures)
        {
            creature.BlockChanged -= handlers.OnBlockChanged;
            creature.PowerIncreased -= handlers.OnPowerIncreased;
            creature.PowerDecreased -= handlers.OnPowerDecreased;
            creature.PowerRemoved -= handlers.OnPowerRemoved;
            creature.Died -= handlers.OnDied;
        }
        _subscribedCreatures.Clear();
    }

    private class CreatureHandlers
    {
        private readonly Creature _creature;

        public CreatureHandlers(Creature creature)
        {
            _creature = creature;
        }

        public void OnBlockChanged(int oldBlock, int newBlock)
        {
            EventDispatcher.Enqueue(new BlockEvent(_creature, oldBlock, newBlock));
        }

        public void OnPowerIncreased(PowerModel power, int change, bool silent)
        {
            if (!silent) EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Increased, change));
        }

        public void OnPowerDecreased(PowerModel power, bool silent)
        {
            if (!silent) EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Decreased));
        }

        public void OnPowerRemoved(PowerModel power)
        {
            EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Removed));
        }

        public void OnDied(Creature c)
        {
            EventDispatcher.Enqueue(new DeathEvent(c));
        }
    }
}
