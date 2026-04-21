using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Number of remote co-op players who have also selected the focused character
/// on the character-select screen. Only rendered when at least one remote
/// player has it selected.
/// </summary>
public sealed class RemoteSelectionAnnouncement : Announcement
{
    private readonly int _count;

    public RemoteSelectionAnnouncement(int count) { _count = count; }

    public override string Key => "remote_selection";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx)
    {
        if (_count <= 0) return Message.Empty;
        return _count == 1
            ? Message.Localized("ui", "CHARACTER.REMOTE_SELECTION_SINGLE")
            : Message.Localized("ui", "CHARACTER.REMOTE_SELECTION_PLURAL", new { count = _count });
    }
}
