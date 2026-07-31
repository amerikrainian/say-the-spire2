using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
using MegaTextLabel = MegaCrit.Sts2.addons.mega_text.MegaLabel;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// Proxy for the bestiary's actions/stats mode toggle button. Its visible
/// caption lives in NBestiary's _modeLabel and always names the view the
/// button switches TO ("View Actions" / "View Stats"), so announcing it reads
/// naturally as the button's action.
///
/// Beta-branch only: _modeLabel does not exist on stable, so this class must
/// never be touched there (BestiaryGameScreen gates on HasStatsSupport) — the
/// `!` lookup intentionally crashes on a rename rather than degrade silently.
/// </summary>
[AnnouncementOrder(typeof(LabelAnnouncement), typeof(TypeAnnouncement))]
public class ProxyBestiaryModeButton : ProxyElement
{
    // The July-31 beta moved the caption label off NBestiary onto the button
    // itself (NBestiaryModeButton._modeLabel, a child node), so the field is
    // nullable and the primary read is the button's own child text.
    private static readonly System.Reflection.FieldInfo? ModeLabelField =
        AccessTools.Field(typeof(NBestiary), "_modeLabel");

    public ProxyBestiaryModeButton(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);
        yield return new TypeAnnouncement("button");
    }

    public override Message? GetLabel()
    {
        if (Control != null && FindChildText(Control) is { Length: > 0 } childText)
            return Message.Raw(StripBbcode(childText));

        var bestiary = NBestiary.Instance;
        if (bestiary != null
            && ModeLabelField?.GetValue(bestiary) is MegaTextLabel label
            && !string.IsNullOrWhiteSpace(label.Text))
        {
            return Message.Raw(StripBbcode(label.Text));
        }
        return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
    }

    public override string? GetTypeKey() => "button";
}
