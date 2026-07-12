using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Generic screen for simple main-menu submenus that are just a vertical list
/// of buttons (singleplayer mode select, multiplayer host/join, multiplayer
/// host mode select). Registers each visible NSubmenuButton in a positioned
/// container so focus announcements gain "2 of 3"-style positions, and
/// rebuilds when button visibility changes (e.g. Load/Abandon appearing on
/// the multiplayer screen).
/// </summary>
public class SubmenuListGameScreen : GameScreen
{
    private readonly NSubmenu _screen;
    private readonly string _locTable;
    private readonly string _locKey;
    private readonly ListContainer _root;
    private int _lastButtonCount = -1;

    public override Message? ScreenName =>
        Message.Raw(ProxyElement.StripBbcode(new LocString(_locTable, _locKey).GetFormattedText()));

    public SubmenuListGameScreen(NSubmenu screen, string locTable, string locKey)
    {
        _screen = screen;
        _locTable = locTable;
        _locKey = locKey;
        _root = new ListContainer
        {
            ContainerLabel = ScreenName,
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = _root;
    }

    protected override void BuildRegistry()
    {
        BuildButtons();
    }

    public override void OnPop()
    {
        base.OnPop();
        _root.Clear();
        _lastButtonCount = -1;
    }

    public override void OnUpdate()
    {
        if (CollectButtons().Count != _lastButtonCount)
            BuildButtons();
    }

    private void BuildButtons()
    {
        _root.Clear();

        var buttons = CollectButtons();
        _lastButtonCount = buttons.Count;
        foreach (var button in buttons)
        {
            var proxy = ProxyFactory.Create(button);
            _root.Add(proxy);
            Register(button, proxy);
        }
    }

    private List<NSubmenuButton> CollectButtons()
    {
        var buttons = new List<NSubmenuButton>();
        CollectButtons(_screen, buttons);
        return buttons;
    }

    private static void CollectButtons(Node node, List<NSubmenuButton> buttons)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is NSubmenuButton button && button.Visible)
                buttons.Add(button);
            CollectButtons(child, buttons);
        }
    }
}
