using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Wraps a pre-composed focus-string Message as a single Announcement. Used as
/// the default GetFocusAnnouncements() implementation on UIElement during the
/// Phase 2 migration: unmigrated proxies surface their old
/// GetLabel / GetExtrasString / GetStatusString / GetTooltip output as one
/// opaque LegacyAnnouncement. Migrated proxies replace it by overriding
/// GetFocusAnnouncements() with structured individual announcements.
/// </summary>
public sealed class LegacyAnnouncement : Announcement
{
    private readonly Message _message;

    public LegacyAnnouncement(Message message)
    {
        _message = message;
    }

    public override string Key => "legacy";
    public override Message Render() => _message;
}
