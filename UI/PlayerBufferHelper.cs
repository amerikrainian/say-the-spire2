using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using SayTheSpire2.Buffers;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI;

public static class PlayerBufferHelper
{
    public static void Populate(BufferManager buffers)
    {
        var playerBuffer = buffers.GetBuffer("player");
        if (playerBuffer == null) return;

        playerBuffer.Clear();

        if (!CombatManager.Instance.IsInProgress) return;

        try
        {
            var combatState = CombatManager.Instance.DebugOnlyGetState();
            if (combatState == null) return;

            var player = LocalContext.GetMe(combatState);
            if (player == null) return;

            var creature = player.Creature;
            var pcs = player.PlayerCombatState;

            // HP
            playerBuffer.Add($"HP: {creature.CurrentHp}/{creature.MaxHp}");

            // Block
            if (creature.Block > 0)
                playerBuffer.Add($"Block: {creature.Block}");

            // Energy
            if (pcs != null)
                playerBuffer.Add($"Energy: {pcs.Energy}/{pcs.MaxEnergy}");

            // Gold
            playerBuffer.Add($"Gold: {player.Gold}");

            // Deck/pile counts
            if (pcs != null)
            {
                playerBuffer.Add($"Draw pile: {pcs.DrawPile.Cards.Count}");
                playerBuffer.Add($"Hand: {pcs.Hand.Cards.Count}");
                playerBuffer.Add($"Discard pile: {pcs.DiscardPile.Cards.Count}");
                if (pcs.ExhaustPile.Cards.Count > 0)
                    playerBuffer.Add($"Exhaust pile: {pcs.ExhaustPile.Cards.Count}");
            }

            // Powers
            if (creature.Powers.Count > 0)
            {
                foreach (var power in creature.Powers)
                {
                    AddPowerToBuffer(playerBuffer, power);
                }
            }
        }
        catch
        {
            // Combat state may not be accessible
        }

        buffers.EnableBuffer("player", true);
    }

    public static void AddPowerToBuffer(Buffer buffer, MegaCrit.Sts2.Core.Models.PowerModel power)
    {
        var title = power.Title.GetFormattedText();
        var amount = power.Amount;
        var line = amount != 0 ? $"{title} {amount}" : title;
        try
        {
            // Use HoverTips to get the fully resolved description (with smart variables)
            // plus any extra tips (e.g. enchantment/affliction tooltips)
            bool first = true;
            foreach (var tip in power.HoverTips)
            {
                if (tip is HoverTip ht)
                {
                    var desc = ht.Description;
                    if (first)
                    {
                        if (!string.IsNullOrEmpty(desc))
                            line += ": " + ProxyElement.StripBbcode(desc);
                        buffer.Add(line);
                        first = false;
                    }
                    else
                    {
                        var extraTitle = ht.Title;
                        var extraLine = !string.IsNullOrEmpty(extraTitle) && !string.IsNullOrEmpty(desc)
                            ? $"{extraTitle}: {ProxyElement.StripBbcode(desc)}"
                            : !string.IsNullOrEmpty(extraTitle) ? extraTitle
                            : ProxyElement.StripBbcode(desc);
                        buffer.Add(extraLine);
                    }
                }
            }
            if (first)
                buffer.Add(line);
        }
        catch
        {
            buffer.Add(line);
        }
    }
}
