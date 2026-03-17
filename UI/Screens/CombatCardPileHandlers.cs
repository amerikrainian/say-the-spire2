using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Handles card pile events (draw, discard, exhaust, shuffle) for combat.
/// Extracted from CombatScreen for clarity.
/// </summary>
internal class CombatCardPileHandlers
{
    private readonly PlayerCombatState _combatState;
    private bool _isShuffling;
    private Task? _shuffleTask;
    private bool _endOfTurnDiscardAnnounced;

    public CombatCardPileHandlers(PlayerCombatState combatState)
    {
        _combatState = combatState;
    }

    public void Subscribe()
    {
        _combatState.Hand.CardAdded += OnHandCardAdded;
        _combatState.DiscardPile.CardAdded += OnDiscardCardAdded;
        _combatState.ExhaustPile.CardAdded += OnExhaustCardAdded;
        _combatState.DrawPile.CardAdded += OnDrawCardAdded;
    }

    public void Unsubscribe()
    {
        _combatState.Hand.CardAdded -= OnHandCardAdded;
        _combatState.DiscardPile.CardAdded -= OnDiscardCardAdded;
        _combatState.ExhaustPile.CardAdded -= OnExhaustCardAdded;
        _combatState.DrawPile.CardAdded -= OnDrawCardAdded;
    }

    public void OnTurnStarted()
    {
        _endOfTurnDiscardAnnounced = false;
    }

    public void OnShuffleStarting()
    {
        _isShuffling = true;
    }

    public void OnShuffleStarted(Task shuffleTask)
    {
        _shuffleTask = shuffleTask;
        shuffleTask.ContinueWith(_ =>
        {
            _isShuffling = false;
            _shuffleTask = null;
            EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.DeckShuffled));
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    private void OnHandCardAdded(CardModel card)
    {
        Log.Info($"[EventDebug] CardPile.HandAdded: {card.Title} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.Drew, card.Title));
    }

    private void OnDiscardCardAdded(CardModel card)
    {
        if (CombatManager.Instance.EndingPlayerTurnPhaseTwo
            && !CombatManager.Instance.IsEnemyTurnStarted)
        {
            if (!_endOfTurnDiscardAnnounced)
            {
                _endOfTurnDiscardAnnounced = true;
                Log.Info($"[EventDebug] CardPile.HandDiscarded handler={GetHashCode()}");
                EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.HandDiscarded));
            }
            return;
        }

        if (RunManager.Instance.ActionExecutor.CurrentlyRunningAction is PlayCardAction pca
            && pca.NetCombatCard.ToCardModelOrNull() == card)
            return;

        Log.Info($"[EventDebug] CardPile.Discarded: {card.Title} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.Discarded, card.Title));
    }

    private void OnExhaustCardAdded(CardModel card)
    {
        Log.Info($"[EventDebug] CardPile.Exhausted: {card.Title} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.Exhausted, card.Title));
    }

    private void OnDrawCardAdded(CardModel card)
    {
        if (_isShuffling) return;
        Log.Info($"[EventDebug] CardPile.AddedToDraw: {card.Title} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.AddedToDraw, card.Title));
    }
}
