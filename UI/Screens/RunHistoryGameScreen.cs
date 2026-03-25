using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using SayTheSpire2.Input;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class RunHistoryGameScreen : GameScreen
{
    private static readonly MethodInfo? SelectPlayerMethod =
        AccessTools.Method(typeof(NRunHistory), "SelectPlayer");

    private readonly NRunHistory _screen;
    private readonly NavigableContainer _root = new()
    {
        ContainerLabel = "Run History",
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly Dictionary<Control, ProxyRunHistoryPlayerIcon> _playerProxyCache = new();
    private readonly Dictionary<Control, ProxyRunHistoryMapPoint> _mapProxyCache = new();
    private string? _stateToken;

    public override string? ScreenName => "Run History";

    public RunHistoryGameScreen(NRunHistory screen)
    {
        _screen = screen;
        RootElement = _root;
        ClaimAction("ui_left");
        ClaimAction("ui_right");
        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
    }

    public override void OnPush()
    {
        base.OnPush();
        _stateToken = BuildStateToken();
        _root.FocusFirst();
    }

    public override void OnPop()
    {
        base.OnPop();
        _root.Clear();
        _playerProxyCache.Clear();
        _mapProxyCache.Clear();
        _stateToken = null;
    }

    public override void OnUpdate()
    {
        var token = BuildStateToken();
        if (token == _stateToken)
            return;

        _stateToken = token;
        ClearRegistry();
        BuildRegistry();
        if (_root.FocusedChild == null || !_root.FocusedChild.IsVisible)
            _root.FocusFirst();
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        return action.Key switch
        {
            "ui_right" or "ui_down" => _root.MoveRelative(1),
            "ui_left" or "ui_up" => _root.MoveRelative(-1),
            _ => _root.HandleAction(action),
        };
    }

    protected override void BuildRegistry()
    {
        _root.Clear();

        RegisterNavigation();
        RegisterPlayers();
        RegisterMapHistory();
        RegisterRelics();
        RegisterDeck();
    }

    private void RegisterNavigation()
    {
        RegisterAction(_screen.GetNodeOrNull<NClickableControl>("LeftArrow"), "Previous run");
        RegisterAction(_screen.GetNodeOrNull<NClickableControl>("RightArrow"), "Next run");
    }

    private void RegisterPlayers()
    {
        var playerContainer = _screen.GetNodeOrNull<Control>("%PlayerIconContainer");
        if (playerContainer == null)
            return;

        foreach (var icon in playerContainer.GetChildren().OfType<NRunHistoryPlayerIcon>())
        {
            var proxy = GetOrCreatePlayerProxy(icon);
            var element = new ActionElement(
                () => $"Player, {proxy.GetLabel()}",
                status: () => proxy.GetStatusString(),
                typeKey: () => proxy.GetTypeKey(),
                isVisible: () => proxy.IsVisible,
                onActivated: () => SelectPlayerMethod?.Invoke(_screen, new object[] { icon }));
            _root.Add(element);
            Register(icon, element);
        }
    }

    private void RegisterMapHistory()
    {
        var mapHistory = _screen.GetNodeOrNull<NMapPointHistory>("%MapPointHistory");
        var actsContainer = mapHistory?.GetNodeOrNull<Control>("%Acts");
        if (actsContainer == null)
            return;

        foreach (var point in actsContainer.GetChildren()
                     .OfType<Node>()
                     .SelectMany(node => node.GetChildren().OfType<NMapPointHistoryEntry>()))
        {
            var proxy = GetOrCreateMapProxy(point);
            var element = new ActionElement(
                () => $"Map, {proxy.GetLabel()}",
                status: () => proxy.GetStatusString(),
                typeKey: () => proxy.GetTypeKey(),
                isVisible: () => proxy.IsVisible);
            _root.Add(element);
            Register(point, element);
        }
    }

    private void RegisterRelics()
    {
        var relicHistory = _screen.GetNodeOrNull<Control>("%RelicHistory");
        var relicsContainer = relicHistory?.GetNodeOrNull<Control>("%RelicsContainer");
        if (relicsContainer == null)
            return;

        foreach (var holder in relicsContainer.GetChildren().OfType<NRelicBasicHolder>())
        {
            var proxy = new ProxyRelicHolder(holder);
            var element = new ActionElement(
                () => $"Relic, {proxy.GetLabel()}",
                status: () => proxy.GetStatusString(),
                tooltip: () => proxy.GetTooltip(),
                typeKey: () => proxy.GetTypeKey(),
                isVisible: () => proxy.IsVisible);
            _root.Add(element);
            Register(holder, element);
        }
    }

    private void RegisterDeck()
    {
        var deckHistory = _screen.GetNodeOrNull<Control>("%DeckHistory");
        var cardContainer = deckHistory?.GetNodeOrNull<Control>("%CardContainer");
        if (cardContainer == null)
            return;

        foreach (var entry in cardContainer.GetChildren().OfType<NDeckHistoryEntry>())
        {
            var proxy = ProxyFactory.Create(entry);
            var element = new ActionElement(
                () => $"Deck, {proxy.GetLabel()}",
                status: () => proxy.GetStatusString(),
                tooltip: () => proxy.GetTooltip(),
                typeKey: () => proxy.GetTypeKey(),
                extras: () => proxy.GetExtrasString(),
                isVisible: () => proxy.IsVisible);
            _root.Add(element);
            Register(entry, element);
        }
    }

    private void RegisterAction(NClickableControl? control, string label)
    {
        if (control == null || !control.Visible)
            return;

        var element = new ActionElement(
            () => label,
            status: () => control.IsEnabled ? null : "Disabled",
            typeKey: () => "button",
            onActivated: () => control.EmitSignal(NClickableControl.SignalName.Released, control));
        _root.Add(element);
        Register(control, element);
    }

    private ProxyRunHistoryPlayerIcon GetOrCreatePlayerProxy(NRunHistoryPlayerIcon icon)
    {
        if (_playerProxyCache.TryGetValue(icon, out var proxy))
            return proxy;

        proxy = new ProxyRunHistoryPlayerIcon(icon);
        _playerProxyCache[icon] = proxy;
        return proxy;
    }

    private ProxyRunHistoryMapPoint GetOrCreateMapProxy(NMapPointHistoryEntry point)
    {
        if (_mapProxyCache.TryGetValue(point, out var proxy))
            return proxy;

        proxy = new ProxyRunHistoryMapPoint(point);
        _mapProxyCache[point] = proxy;
        return proxy;
    }

    private string BuildStateToken()
    {
        var players = _screen.GetNodeOrNull<Control>("%PlayerIconContainer")?.GetChildCount() ?? 0;
        var acts = _screen.GetNodeOrNull<NMapPointHistory>("%MapPointHistory")?.GetNodeOrNull<Control>("%Acts")?.GetChildCount() ?? 0;
        var relics = _screen.GetNodeOrNull<Control>("%RelicHistory")?.GetNodeOrNull<Control>("%RelicsContainer")?.GetChildCount() ?? 0;
        var cards = _screen.GetNodeOrNull<Control>("%DeckHistory")?.GetNodeOrNull<Control>("%CardContainer")?.GetChildCount() ?? 0;
        var hp = _screen.GetNodeOrNull<Label>("%HpLabel")?.Text ?? "";
        var floor = _screen.GetNodeOrNull<Label>("%FloorNumLabel")?.Text ?? "";
        var gold = _screen.GetNodeOrNull<Label>("%GoldLabel")?.Text ?? "";
        var gameMode = _screen.GetNodeOrNull<RichTextLabel>("%GameModeLabel")?.Text ?? "";
        var deathQuote = _screen.GetNodeOrNull<RichTextLabel>("%DeathQuoteLabel")?.Text ?? "";
        return $"{players}|{acts}|{relics}|{cards}|{hp}|{gold}|{floor}|{gameMode}|{deathQuote}";
    }
}
