using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyCharacterButton : ProxyElement
{
    public ProxyCharacterButton(Control control) : base(control) { }

    private NCharacterSelectButton? Button => Control as NCharacterSelectButton;

    public override string? GetLabel()
    {
        var button = Button;
        if (button == null) return CleanNodeName(Control.Name);

        if (button.IsRandom) return "Random";

        var character = button.Character;
        if (character == null) return CleanNodeName(Control.Name);

        if (button.IsLocked)
            return new LocString("main_menu_ui", "CHARACTER_SELECT.locked.title").GetFormattedText();

        return new LocString("characters", character.CharacterSelectTitle).GetFormattedText();
    }

    public override string? GetTypeKey() => null;

    public override string? GetStatusString()
    {
        var button = Button;
        if (button == null || button.IsRandom) return null;

        var character = button.Character;
        if (character == null) return null;

        if (button.IsLocked)
        {
            var unlockText = character.GetUnlockText().GetFormattedText();
            return !string.IsNullOrEmpty(unlockText) ? unlockText : null;
        }

        return $"{character.StartingHp} HP, {character.StartingGold} gold";
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
        var charBuffer = buffers.GetBuffer("character");
        if (charBuffer != null)
        {
            charBuffer.Clear();

            if (button.IsLocked)
            {
                charBuffer.Add(new LocString("main_menu_ui", "CHARACTER_SELECT.locked.title").GetFormattedText());

                var unlockText = character.GetUnlockText().GetFormattedText();
                if (!string.IsNullOrEmpty(unlockText))
                    charBuffer.Add(StripBbcode(unlockText));
            }
            else
            {
                charBuffer.Add(new LocString("characters", character.CharacterSelectTitle).GetFormattedText());

                var desc = new LocString("characters", character.CharacterSelectDesc).GetFormattedText();
                if (!string.IsNullOrEmpty(desc))
                    charBuffer.Add(StripBbcode(desc));

                charBuffer.Add($"HP: {character.StartingHp}");
                charBuffer.Add($"Gold: {character.StartingGold}");
                charBuffer.Add($"Energy: {character.MaxEnergy}");
            }

            buffers.EnableBuffer("character", true);
        }

        // Relic buffer
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
