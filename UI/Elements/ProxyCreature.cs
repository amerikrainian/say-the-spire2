using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

[ModSettings("ui.creature", "UI/Creature")]
public class ProxyCreature : ProxyElement
{
    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("intent_first", "Announce Intent Before HP", false));
    }

    public ProxyCreature(Control control) : base(control) { }

    private CreatureView? GetView() => CreatureView.FromControl(Control);

    public override Message? GetLabel()
    {
        var view = GetView();
        if (view == null) return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
        return Message.Raw(view.Name);
    }

    public override string? GetTypeKey() => "creature";

    public override Message? GetStatusString()
    {
        var view = GetView();
        if (view == null) return null;

        var parts = new List<string>();
        var intentFirst = ModSettings.GetValue<bool>("ui.creature.intent_first");

        var intentSummary = GetIntentSummary(view.Entity, includePrefix: !intentFirst);

        if (intentFirst && !string.IsNullOrEmpty(intentSummary))
            parts.Add(intentSummary);

        parts.Add(Message.Localized("ui", "RESOURCE.HP", new { current = view.CurrentHp, max = view.MaxHp }).Resolve());

        if (view.Block > 0)
            parts.Add(Message.Localized("ui", "RESOURCE.BLOCK", new { amount = view.Block }).Resolve());

        if (!intentFirst && !string.IsNullOrEmpty(intentSummary))
            parts.Add(intentSummary);

        return Message.Raw(string.Join(", ", parts));
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var view = GetView();
        if (view == null) return base.HandleBuffers(buffers);

        // Local player: use the player buffer, bound to null
        if (view.IsLocalPlayer)
        {
            var playerBuffer = buffers.GetBuffer("player") as PlayerBuffer;
            if (playerBuffer != null)
            {
                playerBuffer.Bind(null);
                playerBuffer.Update();
                buffers.EnableBuffer("player", true);
            }
            return "player";
        }

        // Another player in multiplayer: bind the player buffer to them
        if (view.IsPlayer && view.Player != null)
        {
            var playerBuffer = buffers.GetBuffer("player") as PlayerBuffer;
            if (playerBuffer != null)
            {
                playerBuffer.Bind(view.Player);
                playerBuffer.Update();
                buffers.EnableBuffer("player", true);
            }
            return "player";
        }

        var creatureBuffer = buffers.GetBuffer("creature") as CreatureBuffer;
        if (creatureBuffer != null)
        {
            creatureBuffer.Bind(view.Entity);
            creatureBuffer.Update();
            buffers.EnableBuffer("creature", true);
        }
        return "creature";
    }

    /// <summary>
    /// Gets the game's localized intent title. Thin delegation to IntentView.
    /// </summary>
    public static string GetIntentName(AbstractIntent intent) => IntentView.GetIntentName(intent);

    public static string? GetIntentSummary(Creature entity, bool includePrefix = true)
    {
        try
        {
            var view = CreatureView.FromEntity(entity);
            if (view.IsMonster)
                return GetMonsterIntentSummary(view, includePrefix);
            if (view.IsPlayer && view.Player != null)
                return GetPlayerIntentSummary(view, includePrefix);
        }
        catch (Exception e)
        {
            Log.Info($"[AccessibilityMod] Intent summary build failed: {e.Message}");
        }
        return null;
    }

    private static string? GetMonsterIntentSummary(CreatureView view, bool includePrefix)
    {
        var intents = view.MonsterIntents;
        if (intents.Count == 0) return null;

        var summaries = intents.Select(intent =>
            !string.IsNullOrEmpty(intent.Label)
                ? $"{intent.Name} {intent.Label}"
                : intent.Name);

        var joined = string.Join(", ", summaries);
        return includePrefix
            ? LocalizationManager.GetOrDefault("ui", "CREATURE.INTENT_PREFIX", "Intent") + " " + joined
            : joined;
    }

    private static string? GetPlayerIntentSummary(CreatureView view, bool includePrefix)
    {
        var model = view.PlayerHoveredModel;
        if (model == null) return null;

        var summary = GetHoveredModelSummary(model);
        if (string.IsNullOrWhiteSpace(summary)) return null;

        return includePrefix
            ? LocalizationManager.GetOrDefault("ui", "CREATURE.INTENT_PREFIX", "Intent") + " " + summary
            : summary;
    }

    private static string? GetHoveredModelSummary(AbstractModel model)
    {
        return model switch
        {
            CardModel card => GetCardIntentSummary(card),
            RelicModel relic => GetRelicIntentSummary(relic),
            PotionModel potion => GetPotionIntentSummary(potion),
            PowerModel power => GetPowerIntentSummary(power),
            _ => null,
        };
    }

    private static string GetCardIntentSummary(CardModel card)
    {
        var proxy = ProxyCard.FromModel(card);
        var parts = new List<string>();
        var label = proxy.GetLabel()?.Resolve();
        var extras = proxy.GetExtrasString()?.Resolve();
        var subtype = proxy.GetSubtypeKey();

        if (!string.IsNullOrWhiteSpace(label))
            parts.Add(label);
        if (!string.IsNullOrWhiteSpace(extras))
            parts.Add(extras);
        if (!string.IsNullOrWhiteSpace(subtype))
            parts.Add(Message.Localized("ui", "CREATURE.SUBTYPE_CARD", new { subtype }).Resolve());

        return string.Join(", ", parts);
    }

    private static string GetRelicIntentSummary(RelicModel relic)
    {
        var proxy = ProxyRelicHolder.FromModel(relic);
        var parts = new List<string>();
        var label = proxy.GetLabel()?.Resolve();
        var status = proxy.GetStatusString()?.Resolve();

        if (!string.IsNullOrWhiteSpace(label))
            parts.Add(label);
        if (!string.IsNullOrWhiteSpace(status))
            parts.Add(status);

        return string.Join(", ", parts);
    }

    private static string GetPotionIntentSummary(PotionModel potion)
    {
        return ProxyPotionHolder.FromModel(potion).GetLabel()?.Resolve() ?? potion.Title.GetFormattedText();
    }

    private static string GetPowerIntentSummary(PowerModel power)
    {
        var title = power.Title.GetFormattedText();
        if (power.StackType == PowerStackType.Counter && power.DisplayAmount != 0)
            return $"{title} {power.DisplayAmount}";
        return title;
    }
}
