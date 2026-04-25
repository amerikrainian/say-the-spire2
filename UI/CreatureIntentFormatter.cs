using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI;

/// <summary>
/// Formats a creature's next-action summary ("what is this creature about to do")
/// into a composable <see cref="Message"/>. For monsters, this is the queued
/// intents; for players, it's the model they're currently hovering. Lives in
/// UI/ because player-intent formatting dispatches into proxies.
/// </summary>
public static class CreatureIntentFormatter
{
    public static Message? Summary(CreatureView view, bool includePrefix = true)
    {
        try
        {
            if (view.IsMonster)
                return MonsterSummary(view, includePrefix);
            if (view.IsPlayer && view.Player != null)
                return PlayerSummary(view, includePrefix);
        }
        catch (Exception e)
        {
            Log.Info($"[AccessibilityMod] Intent summary build failed: {e.Message}");
        }
        return null;
    }

    private static Message? MonsterSummary(CreatureView view, bool includePrefix)
    {
        var intents = view.MonsterIntents;
        if (intents.Count == 0) return null;

        var summaries = intents.Select(intent =>
            !string.IsNullOrEmpty(intent.Label)
                ? $"{intent.Name} {intent.Label}"
                : intent.Name);

        var joined = Message.Raw(string.Join(", ", summaries));
        return includePrefix
            ? Message.Join(" ", Message.Localized("ui", "CREATURE.INTENT_PREFIX"), joined)
            : joined;
    }

    private static Message? PlayerSummary(CreatureView view, bool includePrefix)
    {
        var model = view.PlayerHoveredModel;
        if (model == null) return null;

        var summary = HoveredModelSummary(model);
        if (summary == null || summary.IsEmpty) return null;

        return includePrefix
            ? Message.Join(" ", Message.Localized("ui", "CREATURE.INTENT_PREFIX"), summary)
            : summary;
    }

    public static Message? HoveredModelSummary(AbstractModel model)
    {
        return model switch
        {
            CardModel card => CardSummary(card),
            RelicModel relic => RelicSummary(relic),
            PotionModel potion => PotionSummary(potion),
            PowerModel power => PowerSummary(power),
            _ => null,
        };
    }

    private static Message CardSummary(CardModel card)
    {
        var proxy = ProxyCard.FromModel(card);
        var parts = new List<Message>();
        var label = proxy.GetLabel();
        var extras = proxy.GetExtrasString();
        var subtype = proxy.GetSubtypeKey();

        if (label is { IsEmpty: false }) parts.Add(label);
        if (extras is { IsEmpty: false }) parts.Add(extras);
        if (!string.IsNullOrWhiteSpace(subtype))
            parts.Add(Message.Localized("ui", "CREATURE.SUBTYPE_CARD", new { subtype }));

        return Message.Join(", ", parts.ToArray());
    }

    private static Message RelicSummary(RelicModel relic)
    {
        var proxy = ProxyRelicHolder.FromModel(relic);
        var parts = new List<Message>();
        var label = proxy.GetLabel();
        var status = proxy.GetStatusString();

        if (label is { IsEmpty: false }) parts.Add(label);
        if (status is { IsEmpty: false }) parts.Add(status);

        return Message.Join(", ", parts.ToArray());
    }

    private static Message PotionSummary(PotionModel potion)
    {
        return ProxyPotionHolder.FromModel(potion).GetLabel()
            ?? Message.Raw(potion.Title.GetFormattedText());
    }

    private static Message PowerSummary(PowerModel power)
    {
        var title = power.Title.GetFormattedText();
        if (power.StackType == PowerStackType.Counter && power.DisplayAmount != 0)
            return Message.Raw($"{title} {power.DisplayAmount}");
        return Message.Raw(title);
    }
}
