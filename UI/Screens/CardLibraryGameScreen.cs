using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using SayTheSpire2.Localization;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CardLibraryGameScreen : GameScreen
{
    private static readonly FieldInfo? CardRowsField =
        typeof(NCardGrid).GetField("_cardRows", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

    private readonly NCardLibrary _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = LocalizationManager.GetOrDefault("ui", "CONTAINERS.CARD_LIBRARY", "Card Library"),
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly HashSet<ulong> _connectedControls = new();
    private string? _stateToken;

    public override string? ScreenName => LocalizationManager.GetOrDefault("ui", "SCREENS.CARD_LIBRARY", "Card Library");

    public CardLibraryGameScreen(NCardLibrary screen)
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

        RegisterSearch();
        RegisterFilters();
        RegisterCards();
    }

    private void RegisterSearch()
    {
        var searchBar = _screen.GetNodeOrNull<NSearchBar>("%SearchBar");
        if (searchBar?.TextArea == null)
            return;

        var element = new ActionElement(
            () => "Search",
            status: () => string.IsNullOrWhiteSpace(searchBar.Text)
                ? "No search text"
                : searchBar.Text);

        Register(searchBar.TextArea, element);
        ConnectFocusSignal(searchBar.TextArea, element);
    }

    private void RegisterFilters()
    {
        var filters = new ListContainer
        {
            ContainerLabel = LocalizationManager.GetOrDefault("ui", "CONTAINERS.FILTERS", "Filters"),
            AnnounceName = true,
            AnnouncePosition = true,
        };

        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%IroncladPool"), "Ironclad");
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%SilentPool"), "Silent");
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%DefectPool"), "Defect");
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%RegentPool"), "Regent");
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%NecrobinderPool"), "Necrobinder");
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%ColorlessPool"), "Colorless");
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%AncientsPool"), "Ancients");
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%MiscPool"), "Miscellaneous");
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%CardTypeSorter"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%AttackType"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%SkillType"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%PowerType"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%OtherType"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%RaritySorter"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%CommonRarity"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%UncommonRarity"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%RareRarity"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%OtherRarity"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%CostSorter"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%Cost0"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%Cost1"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%Cost2"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%Cost3+"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%CostX"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%AlphabetSorter"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%Stats"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%Upgrades"));
        RegisterControl(filters, _screen.GetNodeOrNull<Control>("%MultiplayerCards"));

        if (filters.Children.Count > 0)
            _root.Add(filters);
    }

    private void RegisterCards()
    {
        var grid = _screen.GetNodeOrNull<NCardLibraryGrid>("%CardGrid");
        if (grid == null)
            return;

        var gridContainer = new Elements.GridContainer
        {
            ContainerLabel = GetCardCountLabel(),
            AnnounceName = true,
            AnnouncePosition = true,
        };

        var cardRows = CardRowsField?.GetValue(grid) as System.Collections.IList;
        if (cardRows == null)
            return;

        for (int row = 0; row < cardRows.Count; row++)
        {
            var rowList = cardRows[row] as System.Collections.IList;
            if (rowList == null)
                continue;

            for (int col = 0; col < rowList.Count; col++)
            {
                if (rowList[col] is not NGridCardHolder holder || !holder.Visible)
                    continue;

                var proxy = new ProxyCard(holder);
                gridContainer.Add(proxy, col, row);
                Register(holder, proxy);
            }
        }

        if (gridContainer.Children.Count > 0)
            _root.Add(gridContainer);
    }

    private void RegisterControl(ListContainer container, Control? control, string? overrideLabel = null)
    {
        if (control == null || !control.Visible)
            return;

        var proxy = control switch
        {
            NCardPoolFilter => new ProxyCardPoolFilter(control),
            NCardViewSortButton => new ProxyCardViewSortButton(control),
            _ => ProxyFactory.Create(control)
        };

        if (proxy is ProxyElement element && overrideLabel != null)
            element.OverrideLabel = overrideLabel;

        container.Add(proxy);
        Register(control, proxy);
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
        if (focusOwner == null)
            return;

        var element = GetElement(focusOwner);
        if (element != null)
            UIManager.SetFocusedControl(focusOwner, element);
    }

    private string BuildStateToken()
    {
        var cardCount = GetCardCountLabel();
        var search = _screen.GetNodeOrNull<NSearchBar>("%SearchBar")?.Text ?? "";
        var visibleCards = _screen.GetNodeOrNull<NCardLibraryGrid>("%CardGrid")?.VisibleCards.ToList() ?? new List<CardModel>();
        var firstCard = visibleCards.Count > 0 ? visibleCards[0].Id.ToString() : "";
        var selectedPools = string.Join(",",
            new[] { "%IroncladPool", "%SilentPool", "%DefectPool", "%RegentPool", "%NecrobinderPool", "%ColorlessPool", "%AncientsPool", "%MiscPool" }
                .Select(path => _screen.GetNodeOrNull<NCardPoolFilter>(path))
                .Where(filter => filter?.IsSelected == true)
                .Select(filter => filter!.Name.ToString()));
        return $"{cardCount}|{search}|{visibleCards.Count}|{firstCard}|{selectedPools}";
    }

    private string GetCardCountLabel()
    {
        var label = _screen.GetNodeOrNull<RichTextLabel>("%CardCountLabel");
        if (label != null && !string.IsNullOrWhiteSpace(label.Text))
            return ProxyElement.StripBbcode(label.Text);

        return "Cards";
    }
}
