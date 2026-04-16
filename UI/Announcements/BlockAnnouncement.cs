using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>A creature's current block amount.</summary>
public sealed class BlockAnnouncement : Announcement
{
    private readonly int _amount;

    public BlockAnnouncement(int amount) { _amount = amount; }

    public override string Key => "block";
    public override string Suffix => ",";
    public override Message Render() =>
        Message.Localized("ui", "RESOURCE.BLOCK", new { amount = _amount });
}
