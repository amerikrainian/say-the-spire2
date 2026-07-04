using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace SayTheSpire2.UI;

/// <summary>
/// Localized pile display names, taken from the game's own strings so the
/// mod says what the sighted UI shows.
/// </summary>
public static class PileNames
{
    public static string ForPileType(PileType pileType)
    {
        return pileType switch
        {
            PileType.Draw => Title("DRAW_PILE.title"),
            PileType.Discard => Title("DISCARD_PILE.title"),
            PileType.Exhaust => Title("EXHAUST_PILE.title"),
            _ => pileType.ToString(),
        };
    }

    /// <summary>
    /// The pile title LocStrings include a <c>{Hotkey:choose(None):| ({})}</c>
    /// template that NCombatCardPile resolves by calling <c>Add("Hotkey", …)</c>
    /// with the in-game shortcut key before formatting. Without that, the raw
    /// template ends up in the string (e.g. "Draw Pile {Hotkey:choose(None):|
    /// ({})}"). The mod has its own hotkey bindings and surfaces them through
    /// the help system; we don't want the game's hint baked into the title.
    /// Pass <c>"None"</c> so the template resolves to just the base name.
    /// </summary>
    public static string Title(string key)
    {
        var ls = new LocString("static_hover_tips", key);
        ls.Add("Hotkey", "None");
        return ls.GetFormattedText();
    }
}
