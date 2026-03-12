using Godot;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent.CrystalSphereItems;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;

namespace SayTheSpire2.UI.Elements;

public class ProxyCrystalSphereCell : ProxyElement
{
    public ProxyCrystalSphereCell(Control control) : base(control) { }

    private NCrystalSphereCell? Cell => Control as NCrystalSphereCell;

    private CrystalSphereCell? Entity => Cell?.Entity;

    public override string? GetLabel()
    {
        var entity = Entity;
        if (entity == null) return null;

        if (entity.IsHidden)
            return "Hidden";

        var item = entity.Item;
        if (item == null)
            return "Empty";

        var itemLabel = GetItemLabel(item);
        var rangeStr = GetItemRangeString(item);
        return rangeStr != null ? $"{itemLabel} {rangeStr}" : itemLabel;
    }

    private static string GetItemLabel(CrystalSphereItem item)
    {
        return item switch
        {
            CrystalSphereRelic => "Relic",
            CrystalSpherePotion => "Potion",
            CrystalSphereCardReward => "Card Reward",
            CrystalSphereGold => "Gold",
            CrystalSphereCurse => "Curse",
            _ => "Item"
        };
    }

    private static string? GetItemRangeString(CrystalSphereItem item)
    {
        if (item.Size.X <= 1 && item.Size.Y <= 1)
            return null;

        // Match GridContainer output: X, Y
        int startX = item.Position.X + 1;
        int startY = item.Position.Y + 1;
        int endX = item.Position.X + item.Size.X;
        int endY = item.Position.Y + item.Size.Y;
        return $"from {startX}, {startY} to {endX}, {endY}";
    }
}
