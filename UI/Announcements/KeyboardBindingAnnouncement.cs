using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// An input entry's current keyboard binding, or "keyboard unbound" when no
/// key is assigned. Only yielded when the input supports keyboard remapping.
/// </summary>
public sealed class KeyboardBindingAnnouncement : Announcement
{
    private readonly string? _keyName;

    public KeyboardBindingAnnouncement(string? keyName) { _keyName = keyName; }

    public override string Key => "keyboard_binding";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx)
    {
        return string.IsNullOrEmpty(_keyName)
            ? Message.Localized("ui", "BINDING.KEYBOARD_UNBOUND")
            : Message.Localized("ui", "BINDING.KEYBOARD_BOUND", new { key = _keyName });
    }
}
