using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Rooms;
using SayTheSpire2.Events;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class EventHooks
{
    private static readonly FieldInfo? TitleField =
        typeof(NEventLayout).GetField("_title", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? DialogueContainerField =
        typeof(NAncientEventLayout).GetField("_dialogueContainer", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? EventField =
        typeof(NEventLayout).GetField("_event", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Initialize(Harmony harmony)
    {
        // Event descriptions and dialogue
        PatchIfFound(harmony, typeof(NEventLayout), "SetDescription",
            nameof(SetDescriptionPostfix), "Event SetDescription");
        PatchIfFound(harmony, typeof(NAncientEventLayout), "InitializeVisuals",
            nameof(AncientInitializeVisualsPostfix), "Ancient InitializeVisuals");
        PatchIfFound(harmony, typeof(NAncientEventLayout), "SetDialogueLineAndAnimate",
            nameof(SetDialogueLinePostfix), "Ancient dialogue line");

        // Card events
        PatchIfFound(harmony, typeof(SwipePower), "Steal",
            nameof(CardStolenPostfix), "SwipePower.Steal");
        PatchIfFound(harmony, typeof(CardCmd), "Upgrade",
            nameof(CardUpgradeCombatPostfix), "CardCmd.Upgrade (combat)",
            parameterTypes: new[] { typeof(IEnumerable<CardModel>), typeof(CardPreviewStyle) });
        PatchIfFound(harmony, typeof(MegaCrit.Sts2.Core.Nodes.Vfx.NCardUpgradeVfx), "Create",
            nameof(CardUpgradeVfxPostfix), "NCardUpgradeVfx.Create");
        PatchIfFound(harmony, typeof(MegaCrit.Sts2.Core.Nodes.Vfx.NCardSmithVfx), "Create",
            nameof(CardSmithVfxPostfix), "NCardSmithVfx.Create",
            parameterTypes: new[] { typeof(IEnumerable<CardModel>), typeof(bool) });
        PatchIfFound(harmony, typeof(CardModel), "SpendResources",
            nameof(CardPlayedPrefix), "CardModel.SpendResources", isPrefix: true);

        // Orb events
        PatchIfFound(harmony, typeof(Hook), "AfterOrbChanneled",
            nameof(OrbChanneledPostfix), "Hook.AfterOrbChanneled");
        PatchIfFound(harmony, typeof(Hook), "AfterOrbEvoked",
            nameof(OrbEvokedPostfix), "Hook.AfterOrbEvoked");

        // Potion used
        PatchIfFound(harmony, typeof(PotionModel), "OnUseWrapper",
            nameof(PotionUsedPrefix), "PotionModel.OnUseWrapper", isPrefix: true);

        // Gold changes
        PatchIfFound(harmony, typeof(PlayerCmd), "GainGold",
            nameof(GainGoldPostfix), "PlayerCmd.GainGold");
        PatchIfFound(harmony, typeof(PlayerCmd), "LoseGold",
            nameof(LoseGoldPostfix), "PlayerCmd.LoseGold");

        // Treasure and rooms
        PatchIfFound(harmony, typeof(NTreasureRoomRelicCollection), "InitializeRelics",
            nameof(InitializeRelicsPostfix), "NTreasureRoomRelicCollection.InitializeRelics");
        PatchIfFound(harmony, typeof(Hook), "AfterRoomEntered",
            nameof(RoomEnteredPostfix), "Hook.AfterRoomEntered");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false, System.Type[]? parameterTypes = null)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(EventHooks), handlerName, label, isPrefix, parameterTypes);
    }

    public static void CardUpgradeCombatPostfix(IEnumerable<CardModel> cards)
    {
        try
        {
            // Only announce during combat — out-of-combat upgrades are handled by VFX hooks
            if (!MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress) return;

            foreach (var card in cards)
            {
                var name = card.Title;
                if (!string.IsNullOrEmpty(name))
                    EventDispatcher.Enqueue(new CardUpgradeEvent(name));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card upgrade combat hook error: {e.Message}");
        }
    }

    public static void CardUpgradeVfxPostfix(CardModel card)
    {
        try
        {
            var name = card.Title;
            if (!string.IsNullOrEmpty(name))
                EventDispatcher.Enqueue(new CardUpgradeEvent(name));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card upgrade VFX hook error: {e.Message}");
        }
    }

    public static void CardSmithVfxPostfix(IEnumerable<CardModel> cards)
    {
        try
        {
            foreach (var card in cards)
            {
                var name = card.Title;
                if (!string.IsNullOrEmpty(name))
                    EventDispatcher.Enqueue(new CardUpgradeEvent(name));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card smith VFX hook error: {e.Message}");
        }
    }

    public static void CardStolenPostfix(SwipePower __instance, CardModel card)
    {
        try
        {
            var cardName = card?.Title;
            if (!string.IsNullOrEmpty(cardName))
                CombatScreen.Current?.OnCardStolen(cardName);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card stolen hook error: {e.Message}");
        }
    }

    public static void AncientInitializeVisualsPostfix(NAncientEventLayout __instance)
    {
        try
        {
            var eventModel = EventField?.GetValue(__instance) as AncientEventModel;
            if (eventModel == null) return;

            var title = eventModel.Title?.GetFormattedText();
            var epithet = eventModel.Epithet?.GetFormattedText();

            var text = !string.IsNullOrEmpty(epithet)
                ? $"{title}, {epithet}"
                : title;

            if (!string.IsNullOrEmpty(text))
            {
                Log.Info($"[AccessibilityMod] Ancient event: \"{text}\"");
                SpeechManager.Output(Message.Raw(text));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Ancient InitializeVisuals hook error: {e.Message}");
        }
    }

    public static void SetDialogueLinePostfix(NAncientEventLayout __instance, int lineIndex)
    {
        try
        {
            var container = DialogueContainerField?.GetValue(__instance) as Node;
            if (container == null) return;

            var child = container.GetChildOrNull<Control>(lineIndex);
            if (child == null) return;

            // NAncientDialogueLine has a %Text (MegaRichTextLabel extends RichTextLabel)
            var textNode = child.GetNodeOrNull<RichTextLabel>("%Text");
            if (textNode == null) return;

            var text = textNode.Text;
            if (!string.IsNullOrEmpty(text))
            {
                Log.Info($"[AccessibilityMod] Ancient dialogue: \"{text}\"");
                SpeechManager.Output(Message.Raw(text));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Ancient dialogue hook error: {e.Message}");
        }
    }

    public static void OrbChanneledPostfix(OrbModel orb)
    {
        try
        {
            var name = orb?.Title.GetFormattedText();
            if (!string.IsNullOrEmpty(name))
                EventDispatcher.Enqueue(new OrbEvent(OrbEventType.Channeled, name));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Orb channeled hook error: {e.Message}");
        }
    }

    public static void OrbEvokedPostfix(OrbModel orb)
    {
        try
        {
            var name = orb?.Title.GetFormattedText();
            if (!string.IsNullOrEmpty(name))
                EventDispatcher.Enqueue(new OrbEvent(OrbEventType.Evoked, name));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Orb evoked hook error: {e.Message}");
        }
    }

    public static void PotionUsedPrefix(PotionModel __instance)
    {
        try
        {
            var player = __instance.Owner;
            if (player == null) return;
            var playerName = player.Creature != null
                ? Multiplayer.MultiplayerHelper.GetCreatureName(player.Creature)
                : Multiplayer.MultiplayerHelper.GetPlayerName(player.NetId);
            var potionName = __instance.Title.GetFormattedText();
            if (!string.IsNullOrEmpty(potionName))
                EventDispatcher.Enqueue(new PotionUsedEvent(playerName, potionName, player.Creature));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Potion used hook error: {e.Message}");
        }
    }

    public static void CardPlayedPrefix(CardModel __instance)
    {
        try
        {
            var player = __instance.Owner;
            if (player == null) return;
            var playerName = player.Creature != null
                ? Multiplayer.MultiplayerHelper.GetCreatureName(player.Creature)
                : Multiplayer.MultiplayerHelper.GetPlayerName(player.NetId);
            var cardName = __instance.Title;
            if (!string.IsNullOrEmpty(cardName))
                EventDispatcher.Enqueue(new CardPlayedEvent(playerName, cardName, player.Creature));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card played hook error: {e.Message}");
        }
    }

    public static void GainGoldPostfix(decimal amount, Player player)
    {
        try
        {
            int gained = (int)amount;
            if (gained > 0)
            {
                int newGold = player.Gold;
                EventDispatcher.Enqueue(new GoldEvent(newGold - gained, newGold, player.Creature));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Gold gained hook error: {e.Message}");
        }
    }

    public static void LoseGoldPostfix(decimal amount, Player player)
    {
        try
        {
            int lost = (int)amount;
            if (lost > 0)
            {
                int newGold = player.Gold;
                EventDispatcher.Enqueue(new GoldEvent(newGold + lost, newGold, player.Creature));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Gold lost hook error: {e.Message}");
        }
    }

    public static void InitializeRelicsPostfix(NTreasureRoomRelicCollection __instance)
    {
        try
        {
            var field = AccessTools.Field(typeof(NTreasureRoomRelicCollection), "_isEmptyChest");
            if (field != null && field.GetValue(__instance) is true)
            {
                var text = new MegaCrit.Sts2.Core.Localization.LocString("gameplay_ui", "TREASURE_EMPTY").GetFormattedText();
                if (!string.IsNullOrEmpty(text))
                    SpeechManager.Output(Message.Raw(text));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Empty chest hook error: {e.Message}");
        }
    }

    public static void RoomEnteredPostfix(AbstractRoom room)
    {
        try
        {
            EventDispatcher.Enqueue(new RoomEnteredEvent(room.RoomType));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Room entered hook error: {e.Message}");
        }
    }

    public static void SetDescriptionPostfix(NEventLayout __instance, string description)
    {
        try
        {
            var title = "";
            var titleLabel = TitleField?.GetValue(__instance);
            if (titleLabel != null)
            {
                var textProp = titleLabel.GetType().GetProperty("Text");
                if (textProp != null)
                    title = textProp.GetValue(titleLabel) as string ?? "";
            }

            if (string.IsNullOrEmpty(description)) return;

            var prefix = "";
            try
            {
                var eventModel = EventField?.GetValue(__instance) as MegaCrit.Sts2.Core.Models.EventModel;
                if (eventModel?.IsShared == true)
                    prefix = LocalizationManager.GetOrDefault("ui", "EVENT.SHARED_PREFIX", "Shared event. ");
            }
            catch (System.Exception e) { Log.Error($"[AccessibilityMod] Event shared status check failed: {e.Message}"); }

            var text = prefix + (string.IsNullOrEmpty(title) ? description : $"{title}. {description}");
            Log.Info($"[AccessibilityMod] Event description: \"{text}\"");
            SpeechManager.Output(Message.Raw(text));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Event description hook error: {e.Message}");
        }
    }
}
