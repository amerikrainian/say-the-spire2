using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(ControlValueAnnouncement)
)]
public class ProxyTextInput : ProxyElement
{
    public ProxyTextInput(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("textbox");

        var status = GetStatusString();
        if (status != null)
            yield return new ControlValueAnnouncement(status);
    }

    public override Message? GetLabel()
    {
        if (Control == null) return OverrideLabel != null ? Message.Raw(OverrideLabel) : null;
        return Message.Raw(OverrideLabel ?? FindSiblingLabel(Control) ?? CleanNodeName(Control.Name));
    }

    public override string? GetTypeKey() => "textbox";

    public override Message? GetStatusString()
    {
        if (Control is not LineEdit lineEdit)
            return null;

        var text = string.IsNullOrWhiteSpace(lineEdit.Text) ? lineEdit.PlaceholderText : lineEdit.Text;
        if (string.IsNullOrWhiteSpace(text))
            return lineEdit.Editable ? null : Message.Raw(Ui("TEXTBOX.READ_ONLY"));

        return lineEdit.Editable ? Message.Raw(text) : Message.Raw($"{text}, {Ui("TEXTBOX.READ_ONLY")}");
    }

    protected override void OnFocus()
    {
        if (Control is LineEdit { Editable: true } lineEdit && !lineEdit.IsEditing())
            lineEdit.Edit();
    }

    protected override void OnUnfocus()
    {
        if (Control is LineEdit { Editable: true } lineEdit && lineEdit.IsEditing())
            lineEdit.Unedit();
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }
}
