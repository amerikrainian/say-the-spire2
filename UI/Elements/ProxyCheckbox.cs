using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Elements;

public class ProxyCheckbox : ProxyElement
{
    public ProxyCheckbox(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        if (Control == null) return null;
        var text = OverrideLabel ?? FindChildText(Control) ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name);
        return Message.Raw(text);
    }

    public override string? GetTypeKey() => "checkbox";

    public override Message? GetStatusString()
    {
        bool? isChecked = Control switch
        {
            NTickbox tickbox => tickbox.IsTicked,
            NCardTypeTickbox tickbox => tickbox.IsTicked,
            NCardCostTickbox tickbox => tickbox.IsTicked,
            _ => null
        };

        if (!isChecked.HasValue)
            return null;

        return Message.Localized("ui", isChecked.Value ? "CHECKBOX.CHECKED" : "CHECKBOX.UNCHECKED");
    }

    protected override void OnFocus()
    {
        if (Control is NTickbox tickbox)
            tickbox.Toggled += OnToggled;
        else if (Control is NCardTypeTickbox cardTypeTickbox)
            cardTypeTickbox.Toggled += OnCardTypeToggled;
        else if (Control is NCardCostTickbox costTickbox)
            costTickbox.Released += OnReleased;
    }

    protected override void OnUnfocus()
    {
        if (Control is NTickbox tickbox)
            tickbox.Toggled -= OnToggled;
        else if (Control is NCardTypeTickbox cardTypeTickbox)
            cardTypeTickbox.Toggled -= OnCardTypeToggled;
        else if (Control is NCardCostTickbox costTickbox)
            costTickbox.Released -= OnReleased;
    }

    private void OnToggled(NTickbox tickbox)
    {
        OutputStatus();
    }

    private void OnCardTypeToggled(NCardTypeTickbox tickbox)
    {
        OutputStatus();
    }

    private void OnReleased(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl control)
    {
        var tree = Control?.GetTree();
        if (tree != null)
        {
            tree.CreateTimer(0).Timeout += OutputStatus;
            return;
        }

        OutputStatus();
    }

    private void OutputStatus()
    {
        var status = GetStatusString();
        if (status != null)
            SpeechManager.Output(status);
    }
}
