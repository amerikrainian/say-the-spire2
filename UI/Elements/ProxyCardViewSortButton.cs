using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace SayTheSpire2.UI.Elements;

public class ProxyCardViewSortButton : ProxyElement
{
    public ProxyCardViewSortButton(Control control) : base(control) { }

    public override string? GetLabel()
    {
        if (OverrideLabel != null)
            return OverrideLabel;

        if (Control is NCardViewSortButton button)
            return FindChildText(button.GetNodeOrNull("Label") ?? button) ?? CleanNodeName(button.Name);

        return CleanNodeName(Control!.Name);
    }

    public override string? GetTypeKey() => "button";

    public override string? GetStatusString()
    {
        if (Control is not NCardViewSortButton button)
            return null;

        return button.IsDescending ? "Descending" : "Ascending";
    }
}
