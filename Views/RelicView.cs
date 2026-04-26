using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Views;

/// <summary>
/// Data wrapper over a game RelicModel. Resolves the model from any of the
/// several NRelic*Holder control types, or wraps a model directly.
/// </summary>
public class RelicView
{
    public RelicModel Model { get; }

    private RelicView(RelicModel model) { Model = model; }

    /// <summary>
    /// Resolves the relic model from one of the known relic-holder control types
    /// (inventory, treasure room, basic). Null if the control isn't a relic holder
    /// or the holder has no relic bound.
    /// </summary>
    public static RelicView? FromControl(Control? control)
    {
        var model = control switch
        {
            NRelicInventoryHolder inv => inv.Relic?.Model,
            NTreasureRoomRelicHolder treasure => treasure.Relic?.Model,
            NRelicBasicHolder basic => basic.Relic?.Model,
            _ => null,
        };
        return model == null ? null : new RelicView(model);
    }

    public static RelicView FromModel(RelicModel model) => new(model);

    public string Title => Model.Title.GetFormattedText();
    public int DisplayAmount => Model.DisplayAmount;
    public bool ShowCounter => Model.ShowCounter;
    public RelicStatus Status => Model.Status;
    public bool IsDisabled => Status == RelicStatus.Disabled;

    /// <summary>Bbcode-stripped dynamic description. Null if empty.</summary>
    public string? Description
    {
        get
        {
            var desc = Model.DynamicDescription.GetFormattedText();
            return string.IsNullOrEmpty(desc) ? null : Message.StripBbcode(desc);
        }
    }
}
