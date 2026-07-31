using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// An item's flavor text — the italic lore line sighted players see under
/// the description (currently surfaced for relics). Its own announcement
/// type so users can toggle it globally without losing descriptions.
/// </summary>
[ShowInGlobalSettings]
public sealed class FlavorAnnouncement : Announcement
{
    private readonly string _text;

    public FlavorAnnouncement(string text) { _text = text; }

    public override string Key => "flavor";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx) => Message.Raw(_text);
}
