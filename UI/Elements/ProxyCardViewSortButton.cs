using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Elements;

public class ProxyCardViewSortButton : ProxyElement
{
    public ProxyCardViewSortButton(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        if (OverrideLabel != null)
            return Message.Raw(OverrideLabel);

        if (Control is NCardViewSortButton button)
        {
            var text = FindChildText(button.GetNodeOrNull("Label") ?? button) ?? CleanNodeName(button.Name);
            return Message.Raw(text);
        }

        return Message.Raw(CleanNodeName(Control!.Name));
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        if (Control is not NCardViewSortButton button)
            return null;

        return Message.Localized("ui", button.IsDescending ? "SORT.DESCENDING" : "SORT.ASCENDING");
    }

    protected override void OnFocus()
    {
        if (Control is NCardViewSortButton button)
            button.Released += OnReleased;
    }

    protected override void OnUnfocus()
    {
        if (Control is NCardViewSortButton button)
            button.Released -= OnReleased;
    }

    private void OnReleased(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl control)
    {
        var status = GetStatusString();
        if (status != null)
            SpeechManager.Output(status);
    }
}
