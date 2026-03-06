using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SayTheSpire2.Events;

namespace SayTheSpire2.UI.Screens;

public class CombatScreen : Screen
{
    public static CombatScreen? Current { get; private set; }

    private readonly CombatState _initialState;
    private readonly Dictionary<Creature, CreatureHandlers> _subscribedCreatures = new();

    public CombatScreen(CombatState state)
    {
        _initialState = state;
    }

    public override void OnPush()
    {
        Current = this;
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
        if (Current == this) Current = null;
        Log.Info("[AccessibilityMod] CombatScreen popped.");
    }

    // -- Navigation fixes --

    public void OnCreatureNavigationUpdated(NCombatRoom combatRoom)
    {
        try
        {
            SetCreatureFocusToRelics(combatRoom);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Creature navigation postfix failed: {e.Message}");
        }
    }

    public void OnTargetingStarted()
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;

            foreach (var creature in combatRoom.CreatureNodes)
            {
                if (creature == null) continue;
                var hitbox = creature.Hitbox;
                if (hitbox == null) continue;
                hitbox.FocusNeighborTop = hitbox.GetPath();
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] StartTargeting failed: {e.Message}");
        }
    }

    public void OnTargetingFinished()
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;
            SetCreatureFocusToRelics(combatRoom);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] FinishTargeting failed: {e.Message}");
        }
    }

    public void OnHandLayoutRefreshed(NPlayerHand hand)
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;

            var firstCreature = combatRoom.CreatureNodes
                .FirstOrDefault(c => c != null && c.IsInteractable && c.Hitbox != null);
            if (firstCreature == null) return;

            var creaturePath = firstCreature.Hitbox.GetPath();

            foreach (var holder in hand.ActiveHolders)
            {
                if (holder == null) continue;
                holder.FocusNeighborTop = creaturePath;
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Hand navigation failed: {e.Message}");
        }
    }

    public void OnCardStolen(string cardName)
    {
        EventDispatcher.Enqueue(new CardStolenEvent(cardName));
    }

    // -- Navigation helpers --

    private static void SetCreatureFocusToRelics(NCombatRoom combatRoom)
    {
        var firstRelic = NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes?.FirstOrDefault();
        if (firstRelic == null || !GodotObject.IsInstanceValid(firstRelic)) return;

        var relicPath = firstRelic.GetPath();

        foreach (var creature in combatRoom.CreatureNodes)
        {
            if (creature == null) continue;
            var hitbox = creature.Hitbox;
            if (hitbox == null) continue;
            hitbox.FocusNeighborTop = relicPath;
        }
    }

    // -- Combat event subscriptions --

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
