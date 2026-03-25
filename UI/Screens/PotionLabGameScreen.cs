using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class PotionLabGameScreen : GameScreen
{
    private readonly NPotionLab _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = "Potion Lab",
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly Dictionary<Control, ProxyPotionLabHolder> _proxyCache = new();
    private readonly HashSet<ulong> _connectedControls = new();
    private string? _stateToken;

    public override string? ScreenName => "Potion Lab";

    public PotionLabGameScreen(NPotionLab screen)
    {
        _screen = screen;
        RootElement = _root;
    }

    public override void OnPush()
    {
        base.OnPush();
        _stateToken = BuildStateToken();
    }

    public override void OnPop()
    {
        base.OnPop();
        _root.Clear();
        _proxyCache.Clear();
        _connectedControls.Clear();
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
        AnnounceFocusedControlIfNeeded();
    }

    protected override void BuildRegistry()
    {
        _root.Clear();

        RegisterCategory("%Common");
        RegisterCategory("%Uncommon");
        RegisterCategory("%Rare");
        RegisterCategory("%Special");
    }

    private void RegisterCategory(string path)
    {
        var category = _screen.GetNodeOrNull<NPotionLabCategory>(path);
        if (category == null)
            return;

        var rows = category.GetGridItems();
        if (rows.Count == 0)
            return;

        var grid = new Elements.GridContainer
        {
            ContainerLabel = GetCategoryLabel(category),
            AnnounceName = true,
            AnnouncePosition = true,
        };

        for (int y = 0; y < rows.Count; y++)
        {
            for (int x = 0; x < rows[y].Count; x++)
            {
                if (rows[y][x] is not NLabPotionHolder holder)
                    continue;

                holder.FocusMode = Control.FocusModeEnum.All;
                var proxy = GetOrCreateProxy(holder);
                grid.Add(proxy, x, y);
                Register(holder, proxy);
                ConnectFocusSignal(holder, proxy);
            }
        }

        _root.Add(grid);
    }

    private ProxyPotionLabHolder GetOrCreateProxy(NLabPotionHolder holder)
    {
        if (_proxyCache.TryGetValue(holder, out var proxy))
            return proxy;

        proxy = new ProxyPotionLabHolder(holder);
        _proxyCache[holder] = proxy;
        return proxy;
    }

    private void ConnectFocusSignal(Control control, UIElement element)
    {
        if (!_connectedControls.Add(control.GetInstanceId()))
            return;

        control.FocusEntered += () => UIManager.SetFocusedControl(control, element);
    }

    private void AnnounceFocusedControlIfNeeded()
    {
        var focusOwner = _screen.GetViewport()?.GuiGetFocusOwner();
        if (focusOwner == null || !_screen.IsAncestorOf(focusOwner))
        {
            var first = GetRegisteredControls().Select(pair => pair.Key).FirstOrDefault();
            first?.GrabFocus();
            return;
        }

        var element = GetElement(focusOwner);
        if (element != null)
            UIManager.SetFocusedControl(focusOwner, element);
    }

    private string BuildStateToken()
    {
        return string.Join("|",
            GetCategoryCount("%Common"),
            GetCategoryCount("%Uncommon"),
            GetCategoryCount("%Rare"),
            GetCategoryCount("%Special"));
    }

    private int GetCategoryCount(string path)
    {
        return _screen.GetNodeOrNull<NPotionLabCategory>(path)?.GetGridItems().Sum(r => r.Count) ?? 0;
    }

    private static string GetCategoryLabel(NPotionLabCategory category)
    {
        var header = category.GetNodeOrNull<RichTextLabel>("Header");
        return header == null ? "Potions" : ProxyElement.StripBbcode(header.Text);
    }
}
