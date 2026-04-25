using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class PlayerExpandedStateScreen : Screen
{
    public static PlayerExpandedStateScreen? Current { get; private set; }

    private readonly NMultiplayerPlayerExpandedState _screen;
    private readonly ListContainer _root = new() { AnnounceName = false, AnnouncePosition = false };
    private readonly ListContainer _relicList = new() { ContainerLabel = Message.Localized("ui", "CONTAINERS.RELICS"), AnnounceName = true, AnnouncePosition = true };
    private readonly ListContainer _potionList = new() { ContainerLabel = Message.Localized("ui", "CONTAINERS.POTIONS"), AnnounceName = true, AnnouncePosition = true };
    private readonly ListContainer _cardList = new() { ContainerLabel = Message.Localized("ui", "CONTAINERS.CARDS"), AnnounceName = true, AnnouncePosition = true };
    private readonly Dictionary<Control, UIElement> _elementCache = new();

    public override string? ScreenName => LocalizationManager.GetOrDefault("ui", "SCREENS.PLAYER_DETAILS", "Player Details");

    public PlayerExpandedStateScreen(NMultiplayerPlayerExpandedState screen)
    {
        _screen = screen;
        _root.Add(_relicList);
        _root.Add(_potionList);
        _root.Add(_cardList);
        RootElement = _root;
    }

    public override void OnPush()
    {
        Current = this;
        BuildContainers();
    }

    public override void OnPop()
    {
        _elementCache.Clear();
        if (Current == this) Current = null;
    }

    public override UIElement? GetElement(Control control)
    {
        return _elementCache.TryGetValue(control, out var element) ? element : null;
    }

    private void BuildContainers()
    {
        _relicList.Clear();
        _potionList.Clear();
        _cardList.Clear();
        _elementCache.Clear();

        var relicContainer = _screen.GetNodeOrNull<Control>("%RelicContainer");
        if (relicContainer != null)
        {
            foreach (var child in relicContainer.GetChildren().OfType<Control>())
            {
                var proxy = ProxyFactory.Create(child);
                _relicList.Add(proxy);
                _elementCache[child] = proxy;
            }
        }

        var potionContainer = _screen.GetNodeOrNull<Control>("%PotionContainer");
        if (potionContainer != null)
        {
            foreach (var child in potionContainer.GetChildren().OfType<Control>())
            {
                var proxy = ProxyFactory.Create(child);
                _potionList.Add(proxy);
                _elementCache[child] = proxy;
            }
        }

        var cardContainer = _screen.GetNodeOrNull<Control>("%CardContainer");
        if (cardContainer != null)
        {
            foreach (var child in cardContainer.GetChildren().OfType<Control>())
            {
                var proxy = ProxyFactory.Create(child);
                _cardList.Add(proxy);
                _elementCache[child] = proxy;
            }
        }

        Log.Info($"[AccessibilityMod] PlayerExpandedState built: {_relicList.Children.Count} relics, {_potionList.Children.Count} potions, {_cardList.Children.Count} cards");
    }
}
