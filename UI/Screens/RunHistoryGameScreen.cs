using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class RunHistoryGameScreen : GameScreen
{
    private static readonly MethodInfo? SelectPlayerMethod =
        AccessTools.Method(typeof(NRunHistory), "SelectPlayer");
    private bool _preferNavigationFocus = true;

    private readonly NRunHistory _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = Ui("RUN_HISTORY.SCREEN_NAME"),
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly Dictionary<Control, ProxyRunHistoryPlayerIcon> _playerProxyCache = new();
    private readonly Dictionary<Control, ProxyRunHistoryMapPoint> _mapProxyCache = new();
    private string? _stateToken;

    public override string? ScreenName => Ui("RUN_HISTORY.SCREEN_NAME");

    public RunHistoryGameScreen(NRunHistory screen)
    {
        _screen = screen;
        RootElement = _root;
        ClaimAction("ui_select");
        ClaimAction("ui_accept");
        ClaimAction("mega_view_deck_and_tab_left");
        ClaimAction("mega_view_exhaust_pile_and_tab_right");
    }

    public override void OnPush()
    {
        base.OnPush();
        _stateToken = BuildStateToken();
        EnsureFocus();
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
        if (token != _stateToken)
        {
            _stateToken = token;
            ClearRegistry();
            BuildRegistry();
        }

        EnsureFocus();
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (action.Key == "mega_view_deck_and_tab_left")
            return ChangeRun(-1);

        if (action.Key == "mega_view_exhaust_pile_and_tab_right")
            return ChangeRun(1);

        if (action.Key is not "ui_select" and not "ui_accept")
            return false;

        var focused = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        if (focused is NRunHistoryPlayerIcon icon)
        {
            SelectPlayerMethod?.Invoke(_screen, new object[] { icon });
            return true;
        }

        if (focused is NClickableControl button)
        {
            button.EmitSignal(NClickableControl.SignalName.Released, button);
            return true;
        }

        return false;
    }

    protected override void BuildRegistry()
    {
        _root.Clear();

        var navigation = NewRow(Ui("RUN_HISTORY.ROWS.RUNS"));
        var players = NewRow(Ui("RUN_HISTORY.ROWS.PLAYERS"));
        var summary = NewRow(Ui("RUN_HISTORY.ROWS.SUMMARY"));
        var details = NewRow(Ui("RUN_HISTORY.ROWS.DETAILS"));
        var map = NewRow(Ui("RUN_HISTORY.ROWS.MAP"));
        var potions = NewRow(Ui("RUN_HISTORY.ROWS.POTIONS"));
        var relics = NewRow(Ui("RUN_HISTORY.ROWS.RELICS"));
        var deck = NewRow(Ui("RUN_HISTORY.ROWS.DECK"));
        var quote = NewRow(Ui("RUN_HISTORY.ROWS.OUTCOME"));

        RegisterNavigation(navigation);
        RegisterPlayers(players);
        RegisterSummary(summary);
        RegisterDetails(details);
        RegisterMapHistory(map);
        RegisterPotions(potions);
        RegisterRelics(relics);
        RegisterDeck(deck);
        RegisterQuote(quote);

        AddIfNotEmpty(navigation);
        AddIfNotEmpty(players);
        AddIfNotEmpty(summary);
        AddIfNotEmpty(details);
        AddIfNotEmpty(map);
        AddIfNotEmpty(potions);
        AddIfNotEmpty(relics);
        AddIfNotEmpty(deck);
        AddIfNotEmpty(quote);

        WireFocusNeighbors();
    }

    private static ListContainer NewRow(string label) => new()
    {
        ContainerLabel = label,
        AnnounceName = true,
        AnnouncePosition = true,
    };

    private void AddIfNotEmpty(ListContainer row)
    {
        if (row.Children.Count > 0)
            _root.Add(row);
    }

    private void RegisterNavigation(ListContainer container)
    {
        RegisterStatic(container, _screen.GetNodeOrNull<NClickableControl>("LeftArrow"), Ui("RUN_HISTORY.PREVIOUS_RUN"));
        RegisterStatic(container, _screen.GetNodeOrNull<NClickableControl>("RightArrow"), Ui("RUN_HISTORY.NEXT_RUN"));
    }

    private void RegisterPlayers(ListContainer container)
    {
        var playerContainer = _screen.GetNodeOrNull<Control>("%PlayerIconContainer");
        if (playerContainer == null)
            return;

        foreach (var icon in playerContainer.GetChildren().OfType<NRunHistoryPlayerIcon>())
        {
            var proxy = GetOrCreatePlayerProxy(icon);
            container.Add(proxy);
            Register(icon, proxy);
        }
    }

    private void RegisterSummary(ListContainer container)
    {
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%HpLabel"), Ui("RUN_HISTORY.FIELDS.HP"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%GoldLabel"), Ui("RUN_HISTORY.FIELDS.GOLD"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%FloorNumLabel"), Ui("RUN_HISTORY.FIELDS.FLOOR"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%RunTimeLabel"), Ui("RUN_HISTORY.FIELDS.RUN_TIME"));
    }

    private void RegisterDetails(ListContainer container)
    {
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%DateLabel"), Ui("RUN_HISTORY.FIELDS.DATE"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%SeedLabel"), Ui("RUN_HISTORY.FIELDS.SEED"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%GameModeLabel"), Ui("RUN_HISTORY.FIELDS.MODE"));
        RegisterStatic(container, _screen.GetNodeOrNull<Control>("%BuildLabel"), Ui("RUN_HISTORY.FIELDS.BUILD"));
    }

    private void RegisterMapHistory(ListContainer container)
    {
        var actsContainer = _screen.GetNodeOrNull<NMapPointHistory>("%MapPointHistory")?.GetNodeOrNull<Control>("%Acts");
        if (actsContainer == null)
            return;

        foreach (var point in actsContainer.GetChildren()
                     .OfType<NActHistoryEntry>()
                     .SelectMany(act => act.Entries))
        {
            var proxy = GetOrCreateMapProxy(point);
            container.Add(proxy);
            Register(point, proxy);
        }
    }

    private void RegisterPotions(ListContainer container)
    {
        var potionContainer = _screen.GetNodeOrNull<Control>("%PotionHolders");
        if (potionContainer == null)
            return;

        foreach (var holder in potionContainer.GetChildren().OfType<NPotionHolder>())
        {
            var proxy = new ProxyPotionHolder(holder);
            container.Add(proxy);
            Register(holder, proxy);
        }
    }

    private void RegisterRelics(ListContainer container)
    {
        var relicsContainer = _screen.GetNodeOrNull<Control>("%RelicHistory")?.GetNodeOrNull<Control>("%RelicsContainer");
        if (relicsContainer == null)
            return;

        foreach (var holder in relicsContainer.GetChildren().OfType<NRelicBasicHolder>())
        {
            var proxy = new ProxyRelicHolder(holder);
            container.Add(proxy);
            Register(holder, proxy);
        }
    }

    private void RegisterDeck(ListContainer container)
    {
        var cardContainer = _screen.GetNodeOrNull<Control>("%DeckHistory")?.GetNodeOrNull<Control>("%CardContainer");
        if (cardContainer == null)
            return;

        foreach (var entry in cardContainer.GetChildren().OfType<NDeckHistoryEntry>())
        {
            var proxy = new ProxyDeckHistoryEntry(entry);
            container.Add(proxy);
            Register(entry, proxy);
        }
    }

    private void RegisterQuote(ListContainer container)
    {
        var control = _screen.GetNodeOrNull<Control>("%DeathQuoteLabel");
        if (control == null || !control.Visible)
            return;

        control.FocusMode = Control.FocusModeEnum.All;
        var element = new ActionElement(
            () => null,
            status: () => GetStaticStatus(Ui("RUN_HISTORY.ROWS.OUTCOME"), control));
        ConnectFocusSignal(control, element);
        container.Add(element);
        Register(control, element);
    }

    private void RegisterStatic(ListContainer container, Control? control, string label)
    {
        if (control == null || !control.Visible)
            return;

        control.FocusMode = Control.FocusModeEnum.All;
        var element = new ActionElement(
            () => label,
            status: () => GetStaticStatus(label, control));
        ConnectFocusSignal(control, element);
        container.Add(element);
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
        var potions = _screen.GetNodeOrNull<Control>("%PotionHolders")?.GetChildCount() ?? 0;
        var relics = _screen.GetNodeOrNull<Control>("%RelicHistory")?.GetNodeOrNull<Control>("%RelicsContainer")?.GetChildCount() ?? 0;
        var cards = _screen.GetNodeOrNull<Control>("%DeckHistory")?.GetNodeOrNull<Control>("%CardContainer")?.GetChildCount() ?? 0;
        return string.Join("|",
            players, acts, potions, relics, cards,
            GetStaticText(_screen.GetNodeOrNull<Control>("%HpLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%GoldLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%FloorNumLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%RunTimeLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%DateLabel")) ?? "",
            GetStaticText(_screen.GetNodeOrNull<Control>("%SeedLabel")) ?? "");
    }

    private void WireFocusNeighbors()
    {
        var rows = new List<List<Control>>
        {
            GetControls("LeftArrow", "RightArrow"),
            GetContainerControls("%PlayerIconContainer"),
            GetControls("%HpLabel", "%GoldLabel", "%FloorNumLabel", "%RunTimeLabel"),
            GetControls("%DateLabel", "%SeedLabel", "%GameModeLabel", "%BuildLabel"),
            GetMapRows(),
            GetContainerControls("%PotionHolders"),
            GetContainerControls("%RelicHistory", "%RelicsContainer"),
            GetContainerControls("%DeckHistory", "%CardContainer"),
            GetControls("%DeathQuoteLabel"),
        }.Where(row => row.Count > 0).ToList();

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (int col = 0; col < row.Count; col++)
            {
                var self = row[col].GetPath();
                row[col].FocusNeighborLeft = col > 0 ? row[col - 1].GetPath() : self;
                row[col].FocusNeighborRight = col < row.Count - 1 ? row[col + 1].GetPath() : self;
                row[col].FocusNeighborTop = rowIndex > 0
                    ? rows[rowIndex - 1][System.Math.Min(col, rows[rowIndex - 1].Count - 1)].GetPath()
                    : self;
                row[col].FocusNeighborBottom = rowIndex < rows.Count - 1
                    ? rows[rowIndex + 1][System.Math.Min(col, rows[rowIndex + 1].Count - 1)].GetPath()
                    : self;
            }
        }
    }

    private List<Control> GetControls(params string[] paths)
    {
        return paths.Select(path => _screen.GetNodeOrNull<Control>(path))
            .Where(control => control != null && control.Visible)
            .Cast<Control>()
            .ToList();
    }

    private List<Control> GetContainerControls(string containerPath)
    {
        return _screen.GetNodeOrNull<Control>(containerPath)?.GetChildren().OfType<Control>().Where(control => control.Visible).ToList()
            ?? new List<Control>();
    }

    private List<Control> GetContainerControls(string parentPath, string childPath)
    {
        return _screen.GetNodeOrNull<Control>(parentPath)?.GetNodeOrNull<Control>(childPath)?.GetChildren().OfType<Control>().Where(control => control.Visible).ToList()
            ?? new List<Control>();
    }

    private List<Control> GetMapRows()
    {
        return _screen.GetNodeOrNull<NMapPointHistory>("%MapPointHistory")?.GetNodeOrNull<Control>("%Acts")
            ?.GetChildren().OfType<NActHistoryEntry>().SelectMany(act => act.Entries).Cast<Control>().ToList()
            ?? new List<Control>();
    }

    private static string? GetStaticText(Control? control)
    {
        return control switch
        {
            RichTextLabel richText => ProxyElement.StripBbcode(richText.Text).Trim(),
            Label label => label.Text.Trim(),
            null => null,
            _ => ProxyElement.FindChildTextPublic(control)?.Trim(),
        };
    }

    private static string? GetStaticStatus(string label, Control? control)
    {
        var text = GetStaticText(control);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var prefixedColon = $"{label}:";
        if (text.StartsWith(prefixedColon))
            return text[prefixedColon.Length..].Trim();

        var prefixedComma = $"{label},";
        if (text.StartsWith(prefixedComma))
            return text[prefixedComma.Length..].Trim();

        if (text == label)
            return null;

        return text;
    }

    private bool ChangeRun(int direction)
    {
        var control = direction < 0
            ? _screen.GetNodeOrNull<NClickableControl>("LeftArrow")
            : _screen.GetNodeOrNull<NClickableControl>("RightArrow");

        if (control == null || !control.Visible || !control.IsEnabled)
            return true;

        _preferNavigationFocus = true;
        control.EmitSignal(NClickableControl.SignalName.Released, control);
        return true;
    }

    private void EnsureFocus()
    {
        var focusOwner = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        if (!_preferNavigationFocus && focusOwner != null && _screen.IsAncestorOf(focusOwner))
            return;

        if (FocusPreferredNavigation())
            _preferNavigationFocus = false;
    }

    private bool FocusPreferredNavigation()
    {
        var previous = _screen.GetNodeOrNull<NClickableControl>("LeftArrow");
        if (TryFocus(previous))
            return true;

        var next = _screen.GetNodeOrNull<NClickableControl>("RightArrow");
        return TryFocus(next);
    }

    private static bool TryFocus(Control? control)
    {
        if (control == null || !control.Visible || control.FocusMode == Control.FocusModeEnum.None)
            return false;

        control.GrabFocus();
        return true;
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }
}
