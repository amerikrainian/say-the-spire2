using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(LockedAnnouncement),
    typeof(StartingHpAnnouncement),
    typeof(StartingGoldAnnouncement),
    typeof(RemoteSelectionAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyCharacterButton : ProxyElement
{
    public ProxyCharacterButton(Control control) : base(control) { }

    private NCharacterSelectButton? Button => Control as NCharacterSelectButton;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        var button = Button;
        var character = button?.Character;

        if (button != null && button.IsLocked)
        {
            yield return new LockedAnnouncement();
        }
        else if (button != null && character != null && !button.IsRandom)
        {
            yield return new StartingHpAnnouncement(character.StartingHp);
            yield return new StartingGoldAnnouncement(character.StartingGold);
            var remoteCount = button.RemoteSelectedPlayers.Count;
            if (remoteCount > 0)
                yield return new RemoteSelectionAnnouncement(remoteCount);
        }

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel()
    {
        var button = Button;
        if (button == null) return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;

        if (button.IsRandom) return Message.Localized("ui", "LABELS.RANDOM");

        var character = button.Character;
        if (character == null) return Message.Raw(CleanNodeName(button.Name));

        if (button.IsLocked)
            return Message.Raw(new LocString("main_menu_ui", "CHARACTER_SELECT.locked.title").GetFormattedText());

        return Message.Raw(new LocString("characters", character.CharacterSelectTitle).GetFormattedText());
    }

    public override string? GetTypeKey() => null;

    public override Message? GetStatusString()
    {
        var button = Button;
        if (button == null) return null;

        var character = button.Character;
        if (character == null) return null;

        if (button.IsLocked)
            return Message.Localized("ui", "LABELS.LOCKED");

        if (button.IsRandom) return null;

        var parts = new System.Collections.Generic.List<Message>
        {
            Message.Localized("ui", "CHARACTER.STARTING_HP", new { amount = character.StartingHp }),
            Message.Localized("ui", "CHARACTER.STARTING_GOLD", new { amount = character.StartingGold }),
        };

        var remoteCount = button.RemoteSelectedPlayers.Count;
        if (remoteCount > 0)
        {
            var remoteKey = remoteCount == 1 ? "CHARACTER.REMOTE_SELECTION_SINGLE" : "CHARACTER.REMOTE_SELECTION_PLURAL";
            parts.Add(Message.Localized("ui", remoteKey, new { count = remoteCount }));
        }

        return Message.Join(", ", parts.ToArray());
    }

    public override Message? GetTooltip()
    {
        var button = Button;
        if (button == null) return null;

        var character = button.Character;
        if (character == null) return null;

        if (button.IsLocked)
        {
            var unlockText = character.GetUnlockText().GetFormattedText();
            return !string.IsNullOrEmpty(unlockText) ? Message.Raw(unlockText) : null;
        }

        var parts = new System.Collections.Generic.List<Message>();

        if (button.IsRandom)
        {
            var desc = new LocString("characters", character.CharacterSelectDesc).GetFormattedText();
            if (!string.IsNullOrEmpty(desc))
                parts.Add(Message.Raw(desc));
        }

        var ascension = GetAscensionText(button);
        if (ascension != null)
            parts.Add(ascension);

        return parts.Count > 0 ? Message.Join(". ", parts.ToArray()) : null;
    }

    private static Message? GetAscensionText(NCharacterSelectButton button)
    {
        Node? node = button;
        while (node != null && node is not NCharacterSelectScreen && node is not NCustomRunScreen)
            node = node.GetParent();
        var panel = node switch
        {
            NCharacterSelectScreen characterSelect => characterSelect.GetNodeOrNull<NAscensionPanel>("%AscensionPanel"),
            NCustomRunScreen customRun => customRun.GetNodeOrNull<NAscensionPanel>("%AscensionPanel"),
            _ => null,
        };
        if (panel != null && panel.Visible)
        {
            var asc = panel.Ascension;
            var title = AscensionHelper.GetTitle(asc).GetFormattedText();
            var description = AscensionHelper.GetDescription(asc).GetFormattedText();
            return Message.Localized("ui", "CHARACTER.ASCENSION_DETAIL", new
            {
                level = asc,
                title,
                description
            });
        }
        return null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var button = Button;
        if (button == null || button.IsRandom)
            return base.HandleBuffers(buffers);

        var character = button.Character;
        if (character == null)
            return base.HandleBuffers(buffers);

        // Character buffer
        var charBuffer = buffers.GetBuffer("character") as CharacterBuffer;
        if (charBuffer != null)
        {
            charBuffer.Bind(button);
            charBuffer.Update();
            buffers.EnableBuffer("character", true);
        }

        // Relic buffer (starting relic for character select)
        var relicBuffer = buffers.GetBuffer("relic");
        if (relicBuffer != null)
        {
            relicBuffer.Clear();

            if (button.IsLocked)
            {
                relicBuffer.Add(new LocString("main_menu_ui", "CHARACTER_SELECT.lockedRelic.title").GetFormattedText());

                var lockedRelicDesc = new LocString("main_menu_ui", "CHARACTER_SELECT.lockedRelic.description").GetFormattedText();
                if (!string.IsNullOrEmpty(lockedRelicDesc))
                    relicBuffer.Add(StripBbcode(lockedRelicDesc));
            }
            else if (character.StartingRelics.Count > 0)
            {
                var relic = character.StartingRelics[0];
                relicBuffer.Add(relic.Title.GetFormattedText());

                var relicDesc = relic.DynamicDescription.GetFormattedText();
                if (!string.IsNullOrEmpty(relicDesc))
                    relicBuffer.Add(StripBbcode(relicDesc));
            }

            buffers.EnableBuffer("relic", true);
        }

        return "character";
    }
}
