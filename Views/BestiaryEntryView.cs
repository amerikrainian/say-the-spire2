using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

namespace SayTheSpire2.Views;

/// <summary>
/// Cross-branch data wrapper over <see cref="NBestiaryEntry"/>. The game's
/// bestiary API diverges between branches, so every divergent member is read
/// via reflection and one build works on both:
/// <list type="bullet">
/// <item>stable/main: <c>NBestiaryEntry.Monster</c> (MonsterModel) + <c>IsLocked</c>.</item>
/// <item>beta: <c>NBestiaryEntry.Entry</c> (a BestiaryEntry with monsterModel,
/// roomType, GetEntryTitle()) + <c>IsDiscovered</c> + <c>IsUnderConstruction</c>.</item>
/// </list>
/// </summary>
public class BestiaryEntryView
{
    // Beta-only members.
    private static readonly PropertyInfo? EntryProp =
        AccessTools.Property(typeof(NBestiaryEntry), "Entry");
    private static readonly PropertyInfo? IsDiscoveredProp =
        AccessTools.Property(typeof(NBestiaryEntry), "IsDiscovered");
    private static readonly PropertyInfo? IsUnderConstructionProp =
        AccessTools.Property(typeof(NBestiaryEntry), "IsUnderConstruction");

    // Stable/main members.
    private static readonly PropertyInfo? MonsterProp =
        AccessTools.Property(typeof(NBestiaryEntry), "Monster");
    private static readonly PropertyInfo? IsLockedProp =
        AccessTools.Property(typeof(NBestiaryEntry), "IsLocked");

    private readonly NBestiaryEntry _entry;

    private BestiaryEntryView(NBestiaryEntry entry) { _entry = entry; }

    public static BestiaryEntryView? FromControl(Control? control) =>
        control is NBestiaryEntry e ? new BestiaryEntryView(e) : null;

    /// <summary>The beta's BestiaryEntry object, or null on stable.</summary>
    private object? BetaEntry => EntryProp?.GetValue(_entry);

    public bool IsUnknown
    {
        get
        {
            if (IsDiscoveredProp?.GetValue(_entry) is bool discovered) return !discovered; // beta
            if (IsLockedProp?.GetValue(_entry) is bool locked) return locked;              // main
            return false;
        }
    }

    /// <summary>Beta-only "under construction" placeholder state; false on main.</summary>
    public bool IsUnderConstruction =>
        IsUnderConstructionProp?.GetValue(_entry) is bool uc && uc;

    /// <summary>
    /// "boss" / "elite" / "monster" — the TYPES.* localization key suffix.
    /// Beta exposes a room-type qualifier on its BestiaryEntry; stable has none,
    /// so everything reads as a plain monster there.
    /// </summary>
    public string TypeKey
    {
        get
        {
            var entry = BetaEntry;
            if (entry == null) return "monster";

            var roomType = AccessTools.Field(entry.GetType(), "roomType")?.GetValue(entry)?.ToString();
            return roomType switch
            {
                "Boss" => "boss",
                "Elite" => "elite",
                _ => "monster",
            };
        }
    }

    /// <summary>
    /// The entry's display title. Beta exposes <c>GetEntryTitle()</c> on its
    /// BestiaryEntry; stable derives it from the monster model's title.
    /// </summary>
    public string EntryTitle
    {
        get
        {
            var entry = BetaEntry;
            if (entry != null)
                return AccessTools.Method(entry.GetType(), "GetEntryTitle")?.Invoke(entry, null) as string ?? "";

            if (MonsterProp?.GetValue(_entry) is MonsterModel monster)
                return monster.Title.GetFormattedText();
            return "";
        }
    }
}
