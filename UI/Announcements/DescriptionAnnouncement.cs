using System.Text;
using System.Text.RegularExpressions;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// The element's description text — used on cards, relics, and potions where
/// the description is the canonical body of the card text, not a hover-tip.
/// Distinct from <see cref="TooltipAnnouncement"/> so it can carry the
/// description-specific <c>collapse_icons</c> setting that condenses runs of
/// the same icon ("Energy Energy Energy" → "3 Energy") without affecting
/// hover tooltips elsewhere in the UI.
/// </summary>
[ShowInGlobalSettings]
public sealed class DescriptionAnnouncement : Announcement
{
    private readonly Message _text;

    public DescriptionAnnouncement(string text) : this(Message.Raw(text)) { }
    public DescriptionAnnouncement(Message text) { _text = text; }

    public override string Key => "description";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx)
    {
        var text = _text.Resolve();
        if (string.IsNullOrEmpty(text)) return _text;

        if (ctx.ResolveBool(Key, "collapse_icons", true))
            text = CollapseIconRuns(text);

        return Message.Raw(text);
    }

    public static void RegisterSettings(CategorySetting category)
    {
        if (category.GetByKey("collapse_icons") == null)
            category.Add(new BoolSetting(
                "collapse_icons",
                "Collapse repeated icons",
                defaultValue: true,
                localizationKey: "SETTINGS.DESCRIPTION.COLLAPSE_ICONS"));
    }

    /// <summary>
    /// Replaces consecutive runs (2+) of the same icon label, optionally
    /// separated by whitespace, with "N label". For example, with the English
    /// "Energy" label: "Gain Energy Energy Energy." → "Gain 3 Energy."
    /// </summary>
    private static string CollapseIconRuns(string text)
    {
        foreach (var label in Message.GetIconLabels())
        {
            if (string.IsNullOrEmpty(label)) continue;

            // Match 2+ occurrences of the label with whitespace allowed ONLY
            // between them — never trailing. The whitespace must sit inside the
            // repeated "\s*{label}" group so the match ends on a label, not on
            // the space that follows the run. Putting it after the group (the
            // old "(?:{label}\s*){2,}") swallowed the space before the next
            // word too, producing "2 Energyat the start" instead of
            // "2 Energy at the start".
            var escaped = Regex.Escape(label);
            var pattern = $"{escaped}(?:\\s*{escaped})+";
            text = Regex.Replace(text, pattern, m =>
            {
                int count = CountOccurrences(m.Value, label);
                return $"{count} {label}";
            });
        }
        return text;
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
