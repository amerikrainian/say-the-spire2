using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class HandSelectGameScreen : GameScreen
{
    public static HandSelectGameScreen? Current { get; private set; }

    private readonly NPlayerHand _hand;
    private readonly string _containerLabel;

    public override string? ScreenName => _containerLabel;

    public HandSelectGameScreen(NPlayerHand hand, string label)
    {
        _hand = hand;
        _containerLabel = label;
    }

    public override void OnPush()
    {
        base.OnPush();
        Current = this;
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    protected override void BuildRegistry()
    {
        var list = new ListContainer
        {
            AnnouncePosition = true,
        };

        foreach (var holder in _hand.ActiveHolders)
        {
            if (holder == null) continue;
            var proxy = new ProxyCard(holder);
            list.Add(proxy);
            Register(holder, proxy);
        }

        RootElement = list;
        Log.Info($"[AccessibilityMod] HandSelectGameScreen built: {list.Children.Count} cards, label: {_containerLabel}");
    }
}
