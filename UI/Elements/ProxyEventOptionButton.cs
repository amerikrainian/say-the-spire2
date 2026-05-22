using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Events;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public class ProxyEventOptionButton : ProxyElement
{
    // User-perceives this as a button; share settings / [AnnouncementOrder] with ProxyButton.
    public override System.Type AnnouncementOrderType => typeof(ProxyButton);

    public ProxyEventOptionButton(Control control) : base(control) { }

    private NEventOptionButton? Button => Control as NEventOptionButton;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("button");

        var option = Button?.Option;
        if (option != null && option.IsLocked)
            yield return new LockedAnnouncement();

        var voters = MultiplayerHelper.GetPlayerNames(Button?.VoteContainer?.Players);
        if (voters.Count > 0)
            yield return new VotersAnnouncement(voters);

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel()
    {
        var option = Button?.Option;
        if (option == null) return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;

        var title = option.Title?.GetFormattedText();
        if (!string.IsNullOrEmpty(title))
            return Message.Raw(StripBbcode(title));

        var desc = option.Description?.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            return Message.Raw(StripBbcode(desc));

        return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        var option = Button?.Option;
        if (option == null) return null;

        return option.IsLocked ? Message.Localized("ui", "LABELS.LOCKED") : null;
    }

    public override Message? GetTooltip()
    {
        var option = Button?.Option;
        if (option == null) return null;

        var desc = option.Description?.GetFormattedText();
        return !string.IsNullOrEmpty(desc) ? Message.Raw(StripBbcode(desc)) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var option = Button?.Option;
        if (option == null) return base.HandleBuffers(buffers);

        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();

            var title = option.Title?.GetFormattedText();
            if (!string.IsNullOrEmpty(title))
                uiBuffer.Add(StripBbcode(title));

            var desc = option.Description?.GetFormattedText();
            if (!string.IsNullOrEmpty(desc))
                uiBuffer.Add(StripBbcode(desc));

            if (option.IsLocked)
                uiBuffer.Add("Locked");

            if (option.Relic != null)
            {
                var relicTitle = option.Relic.Title.GetFormattedText();
                if (!string.IsNullOrEmpty(relicTitle))
                    uiBuffer.Add($"Relic: {relicTitle}");
                try
                {
                    var relicDesc = option.Relic.DynamicDescription.GetFormattedText();
                    if (!string.IsNullOrEmpty(relicDesc))
                        uiBuffer.Add(StripBbcode(relicDesc));
                }
                catch (Exception e) { Log.Error($"[AccessibilityMod] Event option relic description failed: {e.Message}"); }
            }

            // Hover tips (keywords, referenced cards, etc.) — inlined into the
            // ui buffer so the user finds everything about the option in one
            // place, rather than chasing it across card/relic buffers.
            try
            {
                foreach (var tip in option.HoverTips)
                {
                    if (tip is CardHoverTip cardTip)
                    {
                        var formatted = CardBuffer.FormatHoverTip(cardTip.Card);
                        if (!string.IsNullOrEmpty(formatted))
                            uiBuffer.Add(formatted);
                    }
                    else if (tip is HoverTip hoverTip)
                    {
                        var tipTitle = hoverTip.Title;
                        var tipDesc = hoverTip.Description;
                        if (!string.IsNullOrEmpty(tipTitle) && !string.IsNullOrEmpty(tipDesc))
                            uiBuffer.Add($"{tipTitle}: {StripBbcode(tipDesc)}");
                        else if (!string.IsNullOrEmpty(tipTitle))
                            uiBuffer.Add(tipTitle);
                        else if (!string.IsNullOrEmpty(tipDesc))
                            uiBuffer.Add(StripBbcode(tipDesc));
                    }
                }
            }
            catch (Exception e) { Log.Error($"[AccessibilityMod] Event option hover tips failed: {e.Message}"); }

            buffers.EnableBuffer("ui", true);
        }

        return "ui";
    }
}
