using Godot;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyRelicCollectionEntry : ProxyElement
{
    public ProxyRelicCollectionEntry(Control control) : base(control) { }

    private NRelicCollectionEntry? Entry => Control as NRelicCollectionEntry;

    public override string? GetLabel()
    {
        var entry = Entry;
        if (entry == null)
            return null;

        return entry.ModelVisibility == ModelVisibility.Visible
            ? entry.relic.Title.GetFormattedText()
            : "Unknown relic";
    }

    public override string? GetTypeKey() => "relic";

    public override string? GetStatusString()
    {
        return Entry?.ModelVisibility switch
        {
            ModelVisibility.Locked => "Locked",
            ModelVisibility.NotSeen => "Undiscovered",
            _ => null,
        };
    }

    public override string? GetTooltip()
    {
        var entry = Entry;
        if (entry == null)
            return null;

        return entry.ModelVisibility switch
        {
            ModelVisibility.Visible => StripBbcode(entry.relic.DynamicDescription.GetFormattedText()),
            ModelVisibility.NotSeen => new LocString("main_menu_ui", "COMPENDIUM_RELIC_COLLECTION.unknown.description").GetFormattedText(),
            ModelVisibility.Locked => new LocString("main_menu_ui", "COMPENDIUM_RELIC_COLLECTION.locked.description").GetFormattedText(),
            _ => null,
        };
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var entry = Entry;
        if (entry == null || entry.ModelVisibility != ModelVisibility.Visible)
            return base.HandleBuffers(buffers);

        return ProxyRelicHolder.FromModel(entry.relic).HandleBuffers(buffers);
    }
}
