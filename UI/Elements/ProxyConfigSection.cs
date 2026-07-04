using System.Collections.Generic;
using Godot;
using HarmonyLib;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// Proxy for the focusable header of a BaseLib NConfigCollapsibleSection.
/// Announces the section name and expanded/collapsed state; confirm toggles
/// it (BaseLib handles the input itself).
///
/// BaseLib is an optional third-party mod whose versions drift independently
/// of the game and of us, so lookups here degrade gracefully instead of
/// crashing: a missing member just loses that part of the announcement.
/// </summary>
[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(StatusAnnouncement)
)]
public class ProxyConfigSection : ProxyElement
{
    private static readonly System.Type? SectionType =
        AccessTools.TypeByName("BaseLib.Config.UI.NConfigCollapsibleSection");
    private static readonly System.Reflection.FieldInfo? LabelField =
        SectionType != null ? AccessTools.Field(SectionType, "_label") : null;
    private static readonly System.Reflection.PropertyInfo? IsExpandedProperty =
        SectionType != null ? AccessTools.Property(SectionType, "IsExpanded") : null;
    private static readonly System.Reflection.FieldInfo? FocusTargetField =
        SectionType != null ? AccessTools.Field(SectionType, "_focusTarget") : null;

    /// <summary>The section node; Control is its focusable header.</summary>
    private readonly Node _section;

    public ProxyConfigSection(Control headerControl, Node section) : base(headerControl)
    {
        _section = section;
    }

    public static bool IsSection(Node node) =>
        SectionType != null && SectionType.IsInstanceOfType(node);

    public static bool IsExpanded(Node section) =>
        IsExpandedProperty?.GetValue(section) is not false;

    /// <summary>The clickable/focusable header control inside the section.</summary>
    public static Control? GetFocusTarget(Node section) =>
        FocusTargetField?.GetValue(section) as Control;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("button");

        var status = GetStatusString();
        if (status != null)
            yield return new StatusAnnouncement(status);
    }

    public override Message? GetLabel()
    {
        if (GodotObject.IsInstanceValid(_section)
            && LabelField?.GetValue(_section) is RichTextLabel label
            && !string.IsNullOrWhiteSpace(label.Text))
        {
            return Message.Raw(StripBbcode(label.Text));
        }
        return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        if (!GodotObject.IsInstanceValid(_section))
            return null;
        return Message.Localized("ui", IsExpanded(_section) ? "STATUS.EXPANDED" : "STATUS.COLLAPSED");
    }
}
