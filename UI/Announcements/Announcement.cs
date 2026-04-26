using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A single addressable piece of an element's spoken focus message. One class
/// per semantic concept (HpAnnouncement, IntentsAnnouncement, LabelAnnouncement,
/// ...) — the class owns the rendering logic for that concept and is reused
/// everywhere that concept appears.
/// </summary>
public abstract class Announcement
{
    /// <summary>
    /// Stable string identity for this announcement type (e.g., "hp", "label",
    /// "intents"). Used for settings paths and introspection.
    /// </summary>
    public abstract string Key { get; }

    /// <summary>
    /// The announcement's rendered text as a Message. The context gives access
    /// to per-element-resolved setting values (verbose toggles, thresholds, etc.)
    /// — announcements that don't declare custom settings can ignore the param.
    /// </summary>
    public abstract Message Render(AnnouncementContext ctx);

    /// <summary>
    /// Punctuation appended to this announcement's rendered text before the
    /// composer space-joins it with the next announcement. Default is empty
    /// (pure space-join). Subclasses that want a comma after them override this.
    /// </summary>
    public virtual string Suffix => "";
}
