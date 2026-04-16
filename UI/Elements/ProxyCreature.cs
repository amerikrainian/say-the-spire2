using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

        var intentSummary = CreatureIntentFormatter.Summary(view, includePrefix: !intentFirst);

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
    /// Thin delegation kept for CombatScreen / CombatNavigationHooks during the
    /// focus announcements refactor. Callers should migrate to IntentView.GetIntentName directly.
    /// </summary>
    public static string GetIntentName(AbstractIntent intent) => IntentView.GetIntentName(intent);

    /// <summary>
    /// Thin delegation kept for CombatScreen / CombatNavigationHooks during the
    /// focus announcements refactor. Callers should migrate to CreatureIntentFormatter.Summary directly.
    /// </summary>
    public static string? GetIntentSummary(Creature entity, bool includePrefix = true) =>
        CreatureIntentFormatter.Summary(CreatureView.FromEntity(entity), includePrefix);
}
