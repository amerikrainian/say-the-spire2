using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// One read-only stat line in the bestiary's stats view. The game renders all
/// stats in a single rich-text label, so BestiaryGameScreen creates an
/// invisible focusable Control per line and this proxy speaks the matching
/// line (or the character's quote) for the currently selected filter, read
/// live at announce time.
///
/// Beta-branch only: never touched on stable (gated by HasStatsSupport) — the
/// `!` lookup intentionally crashes on a rename rather than degrade silently.
/// </summary>
[AnnouncementOrder(typeof(LabelAnnouncement))]
public class ProxyBestiaryStat : ProxyElement
{
    private static readonly System.Reflection.FieldInfo CurrentFilterField =
        AccessTools.Field(typeof(NBestiary), "_currentFilter")!;

    private readonly int _lineIndex;
    private readonly bool _isQuote;

    public ProxyBestiaryStat(Control control, int lineIndex, bool isQuote) : base(control)
    {
        _lineIndex = lineIndex;
        _isQuote = isQuote;
    }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);
    }

    public override Message? GetLabel()
    {
        var bestiary = NBestiary.Instance;
        var filter = bestiary != null ? CurrentFilterField.GetValue(bestiary) : null;
        if (filter == null) return null;

        if (_isQuote)
        {
            var quote = ProxyBestiaryCharacterFilter.GetQuote(filter);
            return quote != null ? Message.Raw(quote) : null;
        }

        var lines = ProxyBestiaryCharacterFilter.GetStatLines(filter).ToList();
        return _lineIndex < lines.Count ? Message.Raw(lines[_lineIndex]) : null;
    }

    public override string? GetTypeKey() => null;
}
