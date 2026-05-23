using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(StatusAnnouncement)
)]
public class ProxyDropdown : ProxyElement
{
    private static readonly FieldInfo? PositionerDropdownField =
        AccessTools.Field(typeof(NDropdownPositioner), "_dropdownNode");

    public ProxyDropdown(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("dropdown");

        var status = GetStatusString();
        if (status != null)
            yield return new StatusAnnouncement(status);
    }

    public override Message? GetLabel()
    {
        if (Control == null) return null;
        var text = OverrideLabel ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
        return Message.Raw(text);
    }

    public override Message? GetStatusString()
    {
        // The dropdown's selected value lives in a "%Label" MegaLabel under the
        // NDropdown. Settings dropdowns, though, are focused via an
        // NDropdownPositioner that has no value label of its own — its
        // _dropdownNode points at the real NDropdown. Resolve that live (the
        // field can be unset when the screen registry is first built, so we
        // can't rely on the proxy already wrapping the dropdown).
        var dropdown = ResolveDropdownControl();
        if (dropdown == null) return null;

        var labelNode = dropdown.GetNodeOrNull("%Label");
        if (labelNode != null)
        {
            var text = FindChildText(labelNode);
            if (text != null) return Message.Raw(text);
        }

        var childText = FindChildText(dropdown);
        return childText != null ? Message.Raw(childText) : null;
    }

    public override string? GetTypeKey() => "dropdown";

    /// <summary>
    /// The control that actually holds the value "%Label": the wrapped control
    /// itself, or — when we're wrapping an NDropdownPositioner — the NDropdown
    /// it positions, resolved live via reflection.
    /// </summary>
    private Control? ResolveDropdownControl()
    {
        if (Control is NDropdownPositioner positioner)
            return PositionerDropdownField?.GetValue(positioner) as Control ?? Control;
        return Control;
    }
}
