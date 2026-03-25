using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class RelicCollectionGameScreen : GameScreen
{
    private readonly NRelicCollection _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = "Relic Collection",
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly Dictionary<Control, ProxyRelicCollectionEntry> _proxyCache = new();
    private string? _stateToken;

    public override string? ScreenName => "Relic Collection";

    public RelicCollectionGameScreen(NRelicCollection screen)
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
    }

    protected override void BuildRegistry()
    {
        _root.Clear();

        RegisterCategory("%Starter");
        RegisterCategory("%Common");
        RegisterCategory("%Uncommon");
        RegisterCategory("%Rare");
        RegisterCategory("%Shop");
        RegisterCategory("%Ancient");
        RegisterCategory("%Event");
    }

    private void RegisterCategory(string path)
    {
        var category = _screen.GetNodeOrNull<NRelicCollectionCategory>(path);
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
                if (rows[y][x] is not NRelicCollectionEntry entry)
                    continue;

                var proxy = GetOrCreateProxy(entry);
                grid.Add(proxy, x, y);
                Register(entry, proxy);
            }
        }

        _root.Add(grid);
    }

    private ProxyRelicCollectionEntry GetOrCreateProxy(NRelicCollectionEntry entry)
    {
        if (_proxyCache.TryGetValue(entry, out var proxy))
            return proxy;

        proxy = new ProxyRelicCollectionEntry(entry);
        _proxyCache[entry] = proxy;
        return proxy;
    }

    private string BuildStateToken()
    {
        return string.Join("|",
            GetCategoryCount("%Starter"),
            GetCategoryCount("%Common"),
            GetCategoryCount("%Uncommon"),
            GetCategoryCount("%Rare"),
            GetCategoryCount("%Shop"),
            GetCategoryCount("%Ancient"),
            GetCategoryCount("%Event"));
    }

    private int GetCategoryCount(string path)
    {
        return _screen.GetNodeOrNull<NRelicCollectionCategory>(path)?.GetGridItems().Sum(r => r.Count) ?? 0;
    }

    private static string GetCategoryLabel(NRelicCollectionCategory category)
    {
        var header = category.GetNodeOrNull<RichTextLabel>("%Header") ?? category.GetNodeOrNull<RichTextLabel>("Header");
        return header == null ? "Relics" : ProxyElement.StripBbcode(header.Text);
    }
}
