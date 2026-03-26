using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyPotionLabHolder : ProxyElement
{
    private static readonly FieldInfo? ModelField =
        AccessTools.Field(typeof(NLabPotionHolder), "_model");
    private static readonly FieldInfo? VisibilityField =
        AccessTools.Field(typeof(NLabPotionHolder), "_visibility");

    public ProxyPotionLabHolder(Control control) : base(control) { }

    private PotionModel? Model => ModelField?.GetValue(Control) as PotionModel;

    private ModelVisibility GetVisibility()
    {
        return VisibilityField?.GetValue(Control) is ModelVisibility visibility
            ? visibility
            : ModelVisibility.None;
    }

    public override string? GetLabel()
    {
        var model = Model;
        if (model == null)
            return null;

        return GetVisibility() == ModelVisibility.Visible
            ? model.Title.GetFormattedText()
            : "Unknown potion";
    }

    public override string? GetTypeKey() => "potion";

    public override string? GetStatusString()
    {
        return GetVisibility() switch
        {
            ModelVisibility.Locked => "Locked",
            ModelVisibility.NotSeen => "Undiscovered",
            _ => null,
        };
    }

    public override string? GetTooltip()
    {
        var model = Model;
        if (model == null)
            return null;

        return GetVisibility() switch
        {
            ModelVisibility.Visible => StripBbcode(model.DynamicDescription.GetFormattedText()),
            ModelVisibility.NotSeen => new LocString("main_menu_ui", "POTION_LAB_COLLECTION.unknown.description").GetFormattedText(),
            ModelVisibility.Locked => new LocString("main_menu_ui", "POTION_LAB_COLLECTION.locked.description").GetFormattedText(),
            _ => null,
        };
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        if (GetVisibility() != ModelVisibility.Visible || Model == null)
            return base.HandleBuffers(buffers);

        return ProxyPotionHolder.FromModel(Model).HandleBuffers(buffers);
    }
}
