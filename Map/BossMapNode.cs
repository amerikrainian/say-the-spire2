using MegaCrit.Sts2.Core.Map;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Map;

public class BossMapNode : MapNode
{
    public string BossName { get; }

    public BossMapNode(MapPoint point, MapPointState state, string bossName)
        : base(point, state)
    {
        BossName = bossName;
    }

    public override string GetDisplayName() => BossName;
}
