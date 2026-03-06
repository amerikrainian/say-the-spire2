using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace Sts2AccessibilityMod.UI.Elements;

public class ProxyMapPoint : ProxyElement
{
    public ProxyMapPoint(Control control) : base(control) { }

    private NMapPoint? MapPoint => Control as NMapPoint;

    private static string PointTypeName(MapPointType type) => type switch
    {
        MapPointType.Monster => "Monster",
        MapPointType.Elite => "Elite",
        MapPointType.Boss => "Boss",
        MapPointType.Shop => "Shop",
        MapPointType.Treasure => "Treasure",
        MapPointType.RestSite => "Rest Site",
        MapPointType.Ancient => "Ancient",
        MapPointType.Unknown => "Unknown",
        _ => "Unknown",
    };

    public override string? GetLabel()
    {
        var mp = MapPoint;
        if (mp?.Point == null) return CleanNodeName(Control.Name);
        return PointTypeName(mp.Point.PointType);
    }

    public override string? GetTypeKey() => "map node";

    public override string? GetStatusString()
    {
        var mp = MapPoint;
        if (mp?.Point == null) return null;
        return $"({mp.Point.coord.col}, {mp.Point.coord.row})";
    }
}
