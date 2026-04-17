using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Announces that a control is in the locked state. Stateless — the caller only
/// yields this when the control is actually locked.
/// </summary>
public sealed class LockedAnnouncement : Announcement
{
    public override string Key => "locked";
    public override string Suffix => ",";

    public override Message Render() =>
        Message.Localized("ui", "LABELS.LOCKED");
}
