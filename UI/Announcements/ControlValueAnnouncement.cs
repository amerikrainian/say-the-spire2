using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// The current value or state of a control (checkbox "checked"/"unchecked",
/// slider's current value, dropdown's selected option, keybinding summary, etc.).
/// Takes the pre-formatted value Message from the caller.
/// </summary>
public sealed class ControlValueAnnouncement : Announcement
{
    private readonly Message _value;

    public ControlValueAnnouncement(string value) : this(Message.Raw(value)) { }
    public ControlValueAnnouncement(Message value) { _value = value; }

    public override string Key => "control_value";
    public override string Suffix => ",";

    public override Message Render() => _value;
}
