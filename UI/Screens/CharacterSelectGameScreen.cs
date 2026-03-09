using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CharacterSelectGameScreen : GameScreen
{
    public static CharacterSelectGameScreen? Current { get; private set; }

    private readonly NCharacterSelectScreen _screen;

    public override string? ScreenName => "Character Select";

    public CharacterSelectGameScreen(NCharacterSelectScreen screen)
    {
        _screen = screen;
    }

    public override void OnPush()
    {
        Current = this;
        base.OnPush();
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    protected override void BuildRegistry()
    {
        var container = _screen.GetNodeOrNull<Control>("CharSelectButtons/ButtonContainer");
        if (container == null) return;

        var buttons = container.GetChildren().OfType<NCharacterSelectButton>().Where(b => b.Visible).ToList();
        if (buttons.Count == 0) return;

        var list = new ListContainer
        {
            ContainerLabel = "Characters",
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = list;

        foreach (var button in buttons)
        {
            var proxy = new ProxyCharacterButton(button);
            list.Add(proxy);
            Register(button, proxy);
        }

        // Constrain focus so it can't escape the character buttons
        for (int i = 0; i < buttons.Count; i++)
        {
            var self = buttons[i].GetPath();
            buttons[i].FocusNeighborTop = self;
            buttons[i].FocusNeighborBottom = self;
            buttons[i].FocusNeighborLeft = i > 0 ? buttons[i - 1].GetPath() : self;
            buttons[i].FocusNeighborRight = i < buttons.Count - 1 ? buttons[i + 1].GetPath() : self;
        }
    }
}
