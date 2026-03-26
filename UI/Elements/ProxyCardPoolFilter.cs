using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyCardPoolFilter : ProxyElement
{
    public ProxyCardPoolFilter(Control control) : base(control) { }

    public override string? GetLabel()
    {
        if (OverrideLabel != null)
            return OverrideLabel;

        return Control == null ? null : FindChildText(Control) ?? CleanNodeName(Control.Name);
    }

    public override string? GetTypeKey() => "checkbox";

    public override string? GetStatusString()
    {
        if (Control is not NCardPoolFilter filter)
            return null;

        var key = filter.IsSelected ? "CHECKBOX.CHECKED" : "CHECKBOX.UNCHECKED";
        return LocalizationManager.Get("ui", key);
    }

    public override string? GetTooltip()
    {
        if (Control is NCardPoolFilter filter && filter.Loc != null)
            return filter.Loc.GetFormattedText();

        return null;
    }
}
