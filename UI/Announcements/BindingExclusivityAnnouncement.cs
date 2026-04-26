using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Indicates that an input can only be rebound on one device — "keyboard only"
/// (no controller binding is configurable) or "controller only" (no keyboard
/// binding is configurable). Yielded only when exactly one of the two devices
/// supports remapping.
/// </summary>
public sealed class BindingExclusivityAnnouncement : Announcement
{
    private readonly bool _isKeyboardOnly;

    public BindingExclusivityAnnouncement(bool isKeyboardOnly) { _isKeyboardOnly = isKeyboardOnly; }

    public override string Key => "binding_exclusivity";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx) =>
        _isKeyboardOnly
            ? Message.Localized("ui", "BINDING.KEYBOARD_ONLY")
            : Message.Localized("ui", "BINDING.CONTROLLER_ONLY");
}
