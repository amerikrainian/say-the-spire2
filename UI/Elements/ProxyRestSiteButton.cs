using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace SayTheSpire2.UI.Elements;

public class ProxyRestSiteButton : ProxyElement
{
    public ProxyRestSiteButton(Control control) : base(control) { }

    private NRestSiteButton? Button => Control as NRestSiteButton;

    public override string? GetLabel()
    {
        var option = Button?.Option;
        if (option == null) return CleanNodeName(Control.Name);

        return option.Title.GetFormattedText();
    }

    public override string? GetTypeKey() => "button";

    public override string? GetExtrasString()
    {
        var option = Button?.Option;
        if (option == null) return null;

        var desc = option.Description.GetFormattedText();
        return !string.IsNullOrEmpty(desc) ? StripBbcode(desc) : null;
    }
}
