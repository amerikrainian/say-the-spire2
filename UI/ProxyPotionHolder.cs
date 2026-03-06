using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Potions;
using Sts2AccessibilityMod.Buffers;

namespace Sts2AccessibilityMod.UI;

public class ProxyPotionHolder : ProxyElement
{
    public ProxyPotionHolder(Control control) : base(control) { }

    private NPotionHolder? Holder => Control as NPotionHolder;

    public override string? GetLabel()
    {
        var holder = Holder;
        if (holder == null) return CleanNodeName(Control.Name);

        if (!holder.HasPotion)
            return "Empty potion slot";

        return holder.Potion!.Model.Title.GetFormattedText();
    }

    public override string? GetTypeKey() => "potion";

    public override string? GetStatusString()
    {
        var holder = Holder;
        if (holder == null || !holder.HasPotion) return null;

        var desc = holder.Potion!.Model.DynamicDescription.GetFormattedText();
        return !string.IsNullOrEmpty(desc) ? StripBbcode(desc) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var holder = Holder;
        if (holder == null) return base.HandleBuffers(buffers);

        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();

            if (!holder.HasPotion)
            {
                uiBuffer.Add("Empty potion slot");
                // Include the empty slot tooltip
                var slotDesc = new MegaCrit.Sts2.Core.Localization.LocString("static_hover_tips", "POTION_SLOT.description").GetFormattedText();
                if (!string.IsNullOrEmpty(slotDesc))
                    uiBuffer.Add(StripBbcode(slotDesc));
            }
            else
            {
                PopulatePotionBuffer(uiBuffer, holder.Potion!.Model);
            }

            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }

    public static void PopulatePotionBuffer(Buffer buffer, MegaCrit.Sts2.Core.Models.PotionModel model)
    {
        buffer.Add(model.Title.GetFormattedText());

        var desc = model.DynamicDescription.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            buffer.Add(StripBbcode(desc));

        // Hover tips: skip first (it's the potion itself), rest are keywords
        try
        {
            bool first = true;
            foreach (var tip in model.HoverTips)
            {
                if (first) { first = false; continue; }
                if (tip is HoverTip hoverTip)
                {
                    var title = hoverTip.Title;
                    var tipDesc = hoverTip.Description;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(tipDesc))
                        buffer.Add($"{title}: {StripBbcode(tipDesc)}");
                    else if (!string.IsNullOrEmpty(title))
                        buffer.Add(title);
                }
            }
        }
        catch { }
    }
}
