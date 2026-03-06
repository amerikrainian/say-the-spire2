using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyCard : ProxyElement
{
    public ProxyCard(Control control) : base(control) { }

    private NCardHolder? FindCardHolder()
    {
        if (Control is NCardHolder direct)
            return direct;
        Node? current = Control.GetParent();
        while (current != null)
        {
            if (current is NCardHolder holder)
                return holder;
            current = current.GetParent();
        }
        return null;
    }

    private CardModel? GetCardModel() => FindCardHolder()?.CardModel;

    public override string? GetLabel()
    {
        var model = GetCardModel();
        if (model == null) return CleanNodeName(Control.Name);
        return model.Title;
    }

    public override string? GetTypeKey()
    {
        var model = GetCardModel();
        if (model == null) return null;
        return model.Type.ToString().ToLower();
    }

    public override string? GetStatusString()
    {
        var model = GetCardModel();
        if (model == null) return null;

        var parts = new System.Collections.Generic.List<string>();

        // Energy cost
        if (model.EnergyCost != null)
        {
            if (model.EnergyCost.CostsX)
                parts.Add("X energy");
            else
                parts.Add($"{model.EnergyCost.GetWithModifiers(CostModifiers.All)} energy");
        }

        // Star cost
        if (model.CurrentStarCost > 0)
            parts.Add($"{model.CurrentStarCost} stars");

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var model = GetCardModel();
        if (model == null) return base.HandleBuffers(buffers);

        var cardBuffer = buffers.GetBuffer("card");
        if (cardBuffer != null)
        {
            cardBuffer.Clear();

            // Name
            cardBuffer.Add(model.Title);

            // Type
            cardBuffer.Add(model.Type.ToString());

            // Energy cost
            if (model.EnergyCost != null)
            {
                if (model.EnergyCost.CostsX)
                    cardBuffer.Add("Cost: X energy");
                else
                    cardBuffer.Add($"Cost: {model.EnergyCost.GetWithModifiers(CostModifiers.All)} energy");
            }

            // Star cost
            if (model.CurrentStarCost > 0)
                cardBuffer.Add($"Star cost: {model.CurrentStarCost}");

            // Description
            try
            {
                var desc = model.GetDescriptionForPile(PileType.Hand);
                if (!string.IsNullOrEmpty(desc))
                    cardBuffer.Add(StripBbcode(desc));
            }
            catch
            {
                // Hand pile may fail outside combat — try without pile context
                try
                {
                    var desc = model.GetDescriptionForPile(PileType.None);
                    if (!string.IsNullOrEmpty(desc))
                        cardBuffer.Add(StripBbcode(desc));
                }
                catch { }
            }

            // Rarity
            if (model.Rarity != CardRarity.Common)
                cardBuffer.Add(model.Rarity.ToString());

            // Hover tips (keywords, powers, etc.)
            try
            {
                foreach (var tip in model.HoverTips)
                {
                    if (tip is HoverTip hoverTip)
                    {
                        var title = hoverTip.Title;
                        var desc = hoverTip.Description;
                        if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(desc))
                            cardBuffer.Add($"{title}: {StripBbcode(desc)}");
                        else if (!string.IsNullOrEmpty(title))
                            cardBuffer.Add(title);
                        else if (!string.IsNullOrEmpty(desc))
                            cardBuffer.Add(StripBbcode(desc));
                    }
                }
            }
            catch
            {
                // Hover tips may fail outside combat context
            }

            buffers.EnableBuffer("card", true);
        }

        // Upgrade preview buffer
        var upgradeBuffer = buffers.GetBuffer("upgrade");
        if (upgradeBuffer != null)
        {
            upgradeBuffer.Clear();

            if (!model.IsUpgradable)
            {
                upgradeBuffer.Add("No upgrade available");
            }
            else if (model.CardScope != null)
            {
                try
                {
                    var clone = model.CardScope.CloneCard(model);
                    clone.UpgradeInternal();

                    upgradeBuffer.Add(clone.Title);
                    upgradeBuffer.Add(clone.Type.ToString());

                    if (clone.EnergyCost != null)
                    {
                        if (clone.EnergyCost.CostsX)
                            upgradeBuffer.Add("Cost: X energy");
                        else
                            upgradeBuffer.Add($"Cost: {clone.EnergyCost.Canonical} energy");
                    }

                    if (clone.CurrentStarCost > 0)
                        upgradeBuffer.Add($"Star cost: {clone.CurrentStarCost}");

                    try
                    {
                        var desc = clone.GetDescriptionForUpgradePreview();
                        if (!string.IsNullOrEmpty(desc))
                            upgradeBuffer.Add(StripBbcode(desc));
                    }
                    catch { }
                }
                catch (System.Exception e)
                {
                    Log.Error($"[AccessibilityMod] Card upgrade preview failed: {e.Message}");
                    upgradeBuffer.Add("Upgrade preview unavailable");
                }
            }

            buffers.EnableBuffer("upgrade", true);
        }

        // Also populate the player buffer during combat
        PlayerBufferHelper.Populate(buffers);

        return "card";
    }
}
