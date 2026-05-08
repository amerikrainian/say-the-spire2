using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using MegaCrit.Sts2.Core.Rooms;

namespace SayTheSpire2.Views;

/// <summary>
/// Data wrapper over <see cref="NBestiaryEntry"/>. Centralizes access to the
/// bestiary entry's monster, lock state, and room-type qualifier so the rest
/// of the mod doesn't reach into the game node directly.
/// </summary>
public class BestiaryEntryView
{
    private static readonly System.Reflection.FieldInfo MonsterTypeField =
        AccessTools.Field(typeof(NBestiaryEntry), "_monsterType")!;

    public NBestiaryEntry Entry { get; }

    private BestiaryEntryView(NBestiaryEntry entry) { Entry = entry; }

    public static BestiaryEntryView? FromControl(Control? control) =>
        control is NBestiaryEntry entry ? new BestiaryEntryView(entry) : null;

    public MonsterModel? Monster => Entry.Monster;
    public bool IsUnknown => Entry.IsUnknown;
    public bool IsUnderConstruction => Entry.IsUnderConstruction;

    public RoomType MonsterType => (RoomType)MonsterTypeField.GetValue(Entry)!;

    /// <summary>
    /// "boss" / "elite" / "monster" — used as the TYPES.* localization key
    /// suffix on the entry's TypeAnnouncement.
    /// </summary>
    public string TypeKey => MonsterType switch
    {
        RoomType.Boss => "boss",
        RoomType.Elite => "elite",
        _ => "monster",
    };

    /// <summary>The monster's display title. Empty for unknown / under-construction entries.</summary>
    public string MonsterTitle => Monster?.Title.GetFormattedText() ?? "";

    /// <summary>
    /// The under-construction display name. Empty unless <see cref="IsUnderConstruction"/>
    /// is true.
    /// </summary>
    public string UnderConstructionName =>
        Entry.UnderConstructionName?.GetFormattedText() ?? "";
}
