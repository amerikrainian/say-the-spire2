using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// Proxy for one character filter button in the bestiary's stats view.
/// Announces the character name plus selected/locked state, and fills the UI
/// buffer with the game's own STATS.layout lines (encounters / kills / deaths
/// / win rate) and the character's seen-or-kill quote so each stat can be
/// reviewed individually.
///
/// Beta-branch only: stable has no NBestiaryCharacterFilter type, so the game
/// type is resolved by name and its members via reflection. This class must
/// never be touched on stable (BestiaryGameScreen gates on HasStatsSupport) —
/// the `!` lookups below intentionally crash on a rename rather than degrade
/// silently.
/// </summary>
[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(SelectedMarkerAnnouncement),
    typeof(TypeAnnouncement),
    typeof(StatusAnnouncement)
)]
public class ProxyBestiaryCharacterFilter : ProxyElement
{
    private static readonly System.Type FilterType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Screens.Bestiary.NBestiaryCharacterFilter")!;
    private static readonly System.Reflection.FieldInfo KillsField =
        AccessTools.Field(FilterType, "kills")!;
    private static readonly System.Reflection.FieldInfo DeathsField =
        AccessTools.Field(FilterType, "deaths")!;
    private static readonly System.Reflection.FieldInfo CharacterField =
        AccessTools.Field(FilterType, "character")!;
    private static readonly System.Reflection.PropertyInfo IsSelectedProperty =
        AccessTools.Property(FilterType, "IsSelected")!;
    private static readonly System.Reflection.PropertyInfo IsLockedProperty =
        AccessTools.Property(FilterType, "IsLocked")!;
    private static readonly System.Reflection.PropertyInfo WinRateProperty =
        AccessTools.Property(FilterType, "WinRate")!;
    private static readonly System.Reflection.PropertyInfo SeenQuoteProperty =
        AccessTools.Property(FilterType, "BestiarySeenQuote")!;
    private static readonly System.Reflection.PropertyInfo KillQuoteProperty =
        AccessTools.Property(FilterType, "BestiaryKillQuote")!;

    public ProxyBestiaryCharacterFilter(Control control) : base(control) { }

    public static bool IsFilter(Control control) => FilterType.IsInstanceOfType(control);

    public static bool IsSelectedFilter(Control control) =>
        FilterType.IsInstanceOfType(control) && IsSelectedProperty.GetValue(control) is true;

    private object? Filter =>
        Control != null && FilterType.IsInstanceOfType(Control) ? Control : null;

    public static Message GetFilterName(object filter)
    {
        if (CharacterField.GetValue(filter) is CharacterModel character)
            return Message.Raw(StripBbcode(character.Title.GetFormattedText()));
        return Message.Localized("ui", "BESTIARY.ALL_CHARACTERS");
    }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        var filter = Filter;
        if (filter != null && IsSelectedProperty.GetValue(filter) is true)
            yield return new SelectedMarkerAnnouncement();

        yield return new TypeAnnouncement("button");

        var status = GetStatusString();
        if (status != null)
            yield return new StatusAnnouncement(status);
    }

    public override Message? GetLabel()
    {
        var filter = Filter;
        if (filter == null)
            return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
        return GetFilterName(filter);
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        var filter = Filter;
        if (filter != null && IsLockedProperty.GetValue(filter) is true)
            return Message.Localized("ui", "BESTIARY.LOCKED");
        return null;
    }

    /// <summary>
    /// One UI-buffer item per piece of stats-view info: name (with locked
    /// status), each STATS.layout line, then the character's quote about the
    /// selected monster.
    /// </summary>
    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        var filter = Filter;
        if (uiBuffer == null || filter == null) return base.HandleBuffers(buffers);

        uiBuffer.Clear();

        var label = GetLabel()?.Resolve();
        var status = GetStatusString()?.Resolve();
        if (!string.IsNullOrEmpty(label))
            uiBuffer.Add(string.IsNullOrEmpty(status) ? label : $"{label}, {status}");

        foreach (var line in GetStatLines(filter))
            uiBuffer.Add(line);

        var quote = GetQuote(filter);
        if (!string.IsNullOrWhiteSpace(quote))
            uiBuffer.Add(quote);

        buffers.EnableBuffer("ui", true);
        return "ui";
    }

    /// <summary>
    /// Formats the game's own STATS.layout string with this filter's numbers
    /// (mirroring NBestiary.DisplayCharacterData) and splits it into one line
    /// per stat, so the wording matches the sighted UI in every language.
    /// </summary>
    public static IEnumerable<string> GetStatLines(object filter)
    {
        int kills = KillsField.GetValue(filter) is int k ? k : 0;
        int deaths = DeathsField.GetValue(filter) is int d ? d : 0;
        int total = kills + deaths;

        var layout = new LocString("bestiary", "STATS.layout");
        if (total == 0)
        {
            layout.Add("total", 0m);
            layout.Add("kills", 0m);
            layout.Add("deaths", 0m);
            layout.Add("winrate", "--");
        }
        else
        {
            layout.Add("total", total);
            layout.Add("kills", kills);
            layout.Add("deaths", deaths);
            layout.Add("winrate", WinRateProperty.GetValue(filter) as string ?? "--");
        }

        foreach (var line in StripBbcode(layout.GetFormattedText()).Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
                yield return line.Trim();
        }
    }

    /// <summary>
    /// The character's comment on the selected monster: the kill quote once
    /// the character has killed it, otherwise the seen quote. Null for the
    /// all-characters filter, which has no quotes.
    /// </summary>
    public static string? GetQuote(object filter)
    {
        if (CharacterField.GetValue(filter) is not CharacterModel)
            return null;

        int kills = KillsField.GetValue(filter) is int k ? k : 0;
        if (kills > 0)
        {
            var text = (KillQuoteProperty.GetValue(filter) as LocString)?.GetFormattedText()
                ?? new LocString("bestiary", "QUOTE_PLACEHOLDER").GetFormattedText();
            return StripBbcode(text);
        }

        var seen = SeenQuoteProperty.GetValue(filter) as string;
        return string.IsNullOrWhiteSpace(seen) ? null : StripBbcode(seen);
    }
}
