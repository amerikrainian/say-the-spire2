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
                var model = holder.Potion!.Model;
                uiBuffer.Add(model.Title.GetFormattedText());

                var desc = model.DynamicDescription.GetFormattedText();
                if (!string.IsNullOrEmpty(desc))
                    uiBuffer.Add(StripBbcode(desc));

                // Hover tips (extra keywords)
                try
                {
                    foreach (var tip in model.HoverTips)
                    {
                        if (tip is HoverTip hoverTip)
                        {
                            var title = hoverTip.Title;
                            var tipDesc = hoverTip.Description;
                            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(tipDesc))
                                uiBuffer.Add($"{title}: {StripBbcode(tipDesc)}");
                            else if (!string.IsNullOrEmpty(title))
                                uiBuffer.Add(title);
                        }
                    }
                }
                catch { }
            }

            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }
}
