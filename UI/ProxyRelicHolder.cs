using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using Sts2AccessibilityMod.Buffers;

namespace Sts2AccessibilityMod.UI;

public class ProxyRelicHolder : ProxyElement
{
    public ProxyRelicHolder(Control control) : base(control) { }

    private NRelicInventoryHolder? Holder => Control as NRelicInventoryHolder;

    private RelicModel? GetModel()
    {
        var holder = Holder;
        if (holder == null) return null;
        return holder.Relic?.Model;
    }

    public override string? GetLabel()
    {
        var model = GetModel();
        if (model == null) return CleanNodeName(Control.Name);
        return model.Title.GetFormattedText();
    }

    public override string? GetTypeKey() => "relic";

    public override string? GetStatusString()
    {
        var model = GetModel();
        if (model == null) return null;

        var parts = new System.Collections.Generic.List<string>();

        var desc = model.DynamicDescription.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            parts.Add(StripBbcode(desc));

        if (model.ShowCounter && model.DisplayAmount != 0)
            parts.Add($"Counter: {model.DisplayAmount}");

        if (model.Status == RelicStatus.Disabled)
            parts.Add("Disabled");

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var model = GetModel();
        if (model == null) return base.HandleBuffers(buffers);

        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();

            uiBuffer.Add(model.Title.GetFormattedText());

            var desc = model.DynamicDescription.GetFormattedText();
            if (!string.IsNullOrEmpty(desc))
                uiBuffer.Add(StripBbcode(desc));

            if (model.ShowCounter && model.DisplayAmount != 0)
                uiBuffer.Add($"Counter: {model.DisplayAmount}");

            if (model.Status == RelicStatus.Disabled)
                uiBuffer.Add("Disabled");

            // Hover tips
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

            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }
}
