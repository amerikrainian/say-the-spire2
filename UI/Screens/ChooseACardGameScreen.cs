using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class ChooseACardGameScreen : GameScreen
{
    public static ChooseACardGameScreen? Current { get; private set; }

    private readonly NChooseACardSelectionScreen _screen;

    public override string? ScreenName => "Choose a Card";

    public ChooseACardGameScreen(NChooseACardSelectionScreen screen)
    {
        _screen = screen;
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

        var cardRow = _screen.GetNode<Control>("CardRow");
        foreach (var child in cardRow.GetChildren())
        {
            if (child is NGridCardHolder holder)
            {
                var proxy = new ProxyCard(holder);
                list.Add(proxy);
                Register(holder, proxy);
            }
        }

        RootElement = list;
        Log.Info($"[AccessibilityMod] ChooseACardGameScreen built: {list.Children.Count} cards");
    }
}
