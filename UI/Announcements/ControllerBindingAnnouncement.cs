using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// An input entry's current controller binding, or "controller unbound" when
/// no button is assigned. Only yielded when the input supports controller remapping.
/// </summary>
public sealed class ControllerBindingAnnouncement : Announcement
{
    private readonly string? _buttonName;

    public ControllerBindingAnnouncement(string? buttonName) { _buttonName = buttonName; }

    public override string Key => "controller_binding";
    public override string Suffix => ",";

    public override Message Render()
    {
        return string.IsNullOrEmpty(_buttonName)
            ? Message.Localized("ui", "BINDING.CONTROLLER_UNBOUND")
            : Message.Localized("ui", "BINDING.CONTROLLER_BOUND", new { button = _buttonName });
    }
}
