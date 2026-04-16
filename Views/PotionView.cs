using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Views;

/// <summary>
/// Data wrapper over a game PotionModel, or over an empty potion slot (when
/// constructed from a potion holder with no bound potion).
/// </summary>
public class PotionView
{
    public PotionModel? Model { get; }

    /// <summary>True when the view represents a potion slot with no potion bound.</summary>
    public bool IsEmptySlot { get; }

    private PotionView(PotionModel? model, bool isEmptySlot)
    {
        Model = model;
        IsEmptySlot = isEmptySlot;
    }

    /// <summary>
    /// Resolves a view from an NPotionHolder control. Returns an empty-slot view
    /// if the holder has no potion, a bound view if it does. Null for any other
    /// control type.
    /// </summary>
    public static PotionView? FromControl(Control? control)
    {
        if (control is not NPotionHolder holder) return null;
        if (!holder.HasPotion) return new PotionView(null, isEmptySlot: true);
        return new PotionView(holder.Potion!.Model, isEmptySlot: false);
    }

    public static PotionView FromModel(PotionModel model) => new(model, isEmptySlot: false);

    public string? Title => Model?.Title.GetFormattedText();

    /// <summary>Bbcode-stripped dynamic description. Null for empty slots or empty text.</summary>
    public string? Description
    {
        get
        {
            if (Model == null) return null;
            var desc = Model.DynamicDescription.GetFormattedText();
            return string.IsNullOrEmpty(desc) ? null : Message.StripBbcode(desc);
        }
    }
}
