using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CardLibraryGameScreen : GameScreen
{
    private sealed class CardLibraryGridContainer(NCardLibraryGrid grid) : Elements.Container
    {
        private readonly Dictionary<UIElement, NGridCardHolder> _holders = new();
        private int _columnCount = 1;

        public void ClearCards()
        {
            base.Clear();
            _holders.Clear();
            _columnCount = 1;
        }

        public void Upsert(UIElement child, NGridCardHolder holder)
        {
            if (!ReferenceEquals(child.Parent, this))
                base.Add(child);
            _holders[child] = holder;
        }

        public void SetColumnCount(int columnCount)
        {
            _columnCount = columnCount > 0 ? columnCount : 1;
        }

        public override string? GetPositionString(UIElement child)
        {
            if (!_holders.TryGetValue(child, out var holder))
                return null;

            var cardModel = holder.CardModel;
            if (cardModel == null)
                return null;

            var visibleCards = grid.VisibleCards.ToList();
            var index = visibleCards.FindIndex(card => card.Id == cardModel.Id);
            if (index < 0)
                return null;

            return $"{(index % _columnCount) + 1}, {(index / _columnCount) + 1}";
        }
    }

    private static readonly FieldInfo? CardRowsField =
        typeof(NCardGrid).GetField("_cardRows", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

    private readonly NCardLibrary _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = Ui("CARD_LIBRARY.SCREEN_NAME"),
        AnnounceName = true,
        AnnouncePosition = false,
    };
    private readonly HashSet<ulong> _connectedControls = new();
    private readonly Dictionary<Control, UIElement> _elementCache = new();
    private readonly ListContainer _poolRow = NewRow(Ui("CARD_LIBRARY.ROWS.POOLS"));
    private readonly ListContainer _typeRow = NewRow(Ui("CARD_LIBRARY.ROWS.TYPE"));
    private readonly ListContainer _rarityRow = NewRow(Ui("CARD_LIBRARY.ROWS.RARITY"));
    private readonly ListContainer _costRow = NewRow(Ui("CARD_LIBRARY.ROWS.COST"));
    private readonly ListContainer _displayRow = NewRow(Ui("CARD_LIBRARY.ROWS.DISPLAY"));
    private readonly CardLibraryGridContainer _cardGridContainer;
    private readonly Dictionary<ulong, bool> _toggleStates = new();
    private string? _stateToken;
    private bool _suppressOpeningCardFocus;

    public override string? ScreenName => Ui("CARD_LIBRARY.SCREEN_NAME");

    public CardLibraryGameScreen(NCardLibrary screen)
    {
        _screen = screen;
        _cardGridContainer = new CardLibraryGridContainer(screen.GetNodeOrNull<NCardLibraryGrid>("%CardGrid")
            ?? screen.GetNode<NCardLibraryGrid>("%CardGrid"))
        {
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = _root;
        ClaimAction("ui_up", propagate: true);
        ClaimAction("ui_down", propagate: true);
        ClaimAction("ui_left", propagate: true);
        ClaimAction("ui_right", propagate: true);
        ClaimAction("ui_cancel");
        ClaimAction("mega_pause_and_back");
    }

    public override void OnPush()
    {
        _suppressOpeningCardFocus = true;
        base.OnPush();
        _stateToken = BuildStateToken();
        RefreshToggleStateSnapshot();
    }

    public override void OnPop()
    {
        base.OnPop();
        _root.Clear();
        _connectedControls.Clear();
        _elementCache.Clear();
        _toggleStates.Clear();
        _stateToken = null;
        _suppressOpeningCardFocus = false;
    }

    public override void OnUpdate()
    {
        WireFocusNeighbors();

        var token = BuildStateToken();
        if (token == _stateToken)
            return;

        var focusedBeforeRebuild = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        var focusedWasRegistered = focusedBeforeRebuild != null && GetElement(focusedBeforeRebuild) != null;

        _stateToken = token;
        ClearRegistry();
        BuildRegistry();

        var focusedAfterRebuild = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        var sideEffects = GetToggleSideEffectAnnouncements(focusedAfterRebuild);
        var focusedStillRegistered = focusedAfterRebuild != null && GetElement(focusedAfterRebuild) != null;
        if (!focusedWasRegistered || !focusedStillRegistered || focusedBeforeRebuild != focusedAfterRebuild || focusedAfterRebuild is NGridCardHolder)
            AnnounceFocusedControlIfNeeded();
        if (sideEffects.Count > 0)
            SpeechManager.Output(Message.Raw(string.Join(". ", sideEffects)));
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        _suppressOpeningCardFocus = false;

        return action.Key switch
        {
            "ui_cancel" or "mega_pause_and_back" => TryGoBack(),
            _ => false
        };
    }

    protected override void BuildRegistry()
    {
        _root.Clear();
        _poolRow.Clear();
        _typeRow.Clear();
        _rarityRow.Clear();
        _costRow.Clear();
        _displayRow.Clear();

        RegisterSearch();
        RegisterFilters();
        RegisterCards();
        WireFocusNeighbors();
    }

    private void RegisterSearch()
    {
        var textArea = _screen.GetNodeOrNull<NSearchBar>("%SearchBar")?.TextArea;
        if (textArea == null)
            return;

        var element = GetOrCreate(textArea, () =>
        {
            var proxy = new ProxyTextInput(textArea)
            {
                OverrideLabel = Ui("CARD_LIBRARY.SEARCH")
            };
            return proxy;
        });

        RegisterMain(textArea, element);
    }

    private void RegisterFilters()
    {
        RegisterRowControl(_poolRow, _screen.GetNodeOrNull<Control>("%IroncladPool"), "Ironclad");
        RegisterRowControl(_poolRow, _screen.GetNodeOrNull<Control>("%SilentPool"), "Silent");
        RegisterRowControl(_poolRow, _screen.GetNodeOrNull<Control>("%DefectPool"), "Defect");
        RegisterRowControl(_poolRow, _screen.GetNodeOrNull<Control>("%RegentPool"), "Regent");
        RegisterRowControl(_poolRow, _screen.GetNodeOrNull<Control>("%NecrobinderPool"), "Necrobinder");
        RegisterRowControl(_poolRow, _screen.GetNodeOrNull<Control>("%ColorlessPool"), "Colorless");
        RegisterRowControl(_poolRow, _screen.GetNodeOrNull<Control>("%AncientsPool"), "Ancients");
        RegisterRowControl(_poolRow, _screen.GetNodeOrNull<Control>("%MiscPool"), "Miscellaneous");

        RegisterRowControl(_typeRow, _screen.GetNodeOrNull<Control>("%CardTypeSorter"));
        RegisterRowControl(_typeRow, _screen.GetNodeOrNull<Control>("%AttackType"));
        RegisterRowControl(_typeRow, _screen.GetNodeOrNull<Control>("%SkillType"));
        RegisterRowControl(_typeRow, _screen.GetNodeOrNull<Control>("%PowerType"));
        RegisterRowControl(_typeRow, _screen.GetNodeOrNull<Control>("%OtherType"));

        RegisterRowControl(_rarityRow, _screen.GetNodeOrNull<Control>("%RaritySorter"), string.Empty);
        RegisterRowControl(_rarityRow, _screen.GetNodeOrNull<Control>("%CommonRarity"));
        RegisterRowControl(_rarityRow, _screen.GetNodeOrNull<Control>("%UncommonRarity"));
        RegisterRowControl(_rarityRow, _screen.GetNodeOrNull<Control>("%RareRarity"));
        RegisterRowControl(_rarityRow, _screen.GetNodeOrNull<Control>("%OtherRarity"));

        RegisterRowControl(_costRow, _screen.GetNodeOrNull<Control>("%CostSorter"), string.Empty);
        RegisterRowControl(_costRow, _screen.GetNodeOrNull<Control>("%Cost0"));
        RegisterRowControl(_costRow, _screen.GetNodeOrNull<Control>("%Cost1"));
        RegisterRowControl(_costRow, _screen.GetNodeOrNull<Control>("%Cost2"));
        RegisterRowControl(_costRow, _screen.GetNodeOrNull<Control>("%Cost3+"));
        RegisterRowControl(_costRow, _screen.GetNodeOrNull<Control>("%CostX"));

        RegisterRowControl(_displayRow, _screen.GetNodeOrNull<Control>("%AlphabetSorter"));
        RegisterRowControl(_displayRow, _screen.GetNodeOrNull<Control>("%Stats"));
        RegisterRowControl(_displayRow, _screen.GetNodeOrNull<Control>("%Upgrades"));
        RegisterRowControl(_displayRow, _screen.GetNodeOrNull<Control>("%MultiplayerCards"));

        AddRowIfNotEmpty(_poolRow);
        AddRowIfNotEmpty(_typeRow);
        AddRowIfNotEmpty(_rarityRow);
        AddRowIfNotEmpty(_costRow);
        AddRowIfNotEmpty(_displayRow);
    }

    private void RegisterCards()
    {
        var grid = _screen.GetNodeOrNull<NCardLibraryGrid>("%CardGrid");
        if (grid == null)
            return;

        _cardGridContainer.ClearCards();
        _cardGridContainer.ContainerLabel = GetCardCountLabel();

        var cardRows = CardRowsField?.GetValue(grid) as System.Collections.IList;
        if (cardRows == null)
            return;

        var columnCount = GetGridColumnCount(cardRows);
        _cardGridContainer.SetColumnCount(columnCount);

        for (int row = 0; row < cardRows.Count; row++)
        {
            var rowList = cardRows[row] as System.Collections.IList;
            if (rowList == null)
                continue;

            for (int col = 0; col < rowList.Count; col++)
            {
                if (rowList[col] is not NGridCardHolder holder || !holder.Visible)
                    continue;

                var proxy = GetOrCreate(holder, () => new ProxyCard(holder));
                _cardGridContainer.Upsert(proxy, holder);
                Register(holder, proxy);
                ConnectFocusSignal(holder, proxy);
            }
        }

        if (_cardGridContainer.Children.Count > 0)
            _root.Add(_cardGridContainer);
    }

    private void RegisterRowControl(ListContainer row, Control? control, string? overrideLabel = null)
    {
        if (!IsUsable(control))
            return;

        var element = GetOrCreate(control!, () => CreateElement(control!, overrideLabel));
        row.Add(element);
        Register(control!, element);
        ConnectFocusSignal(control!, element);
    }

    private void RegisterMain(Control control, UIElement element)
    {
        control.FocusMode = Control.FocusModeEnum.All;
        Register(control, element);
        ConnectFocusSignal(control, element);
        _root.Add(element);
    }

    private UIElement CreateElement(Control control, string? overrideLabel)
    {
        UIElement element = control switch
        {
            NCardPoolFilter => new ProxyCardPoolFilter(control),
            NCardViewSortButton => new ProxyCardViewSortButton(control),
            _ => ProxyFactory.Create(control)
        };

        if (element is ProxyElement proxy && overrideLabel != null)
            proxy.OverrideLabel = overrideLabel;

        return element;
    }

    private T GetOrCreate<T>(Control control, System.Func<T> factory) where T : UIElement
    {
        if (_elementCache.TryGetValue(control, out var existing) && existing is T typed)
            return typed;

        var created = factory();
        _elementCache[control] = created;
        return created;
    }

    private void ConnectFocusSignal(Control control, UIElement element)
    {
        if (!_connectedControls.Add(control.GetInstanceId()))
            return;

        control.FocusEntered += () =>
        {
            if (ShouldSuppressOpeningCardFocus(control))
                return;

            if (control is NGridCardHolder holder)
                ResolveLiveCardHolder(holder);

            UIManager.SetFocusedControl(control, element);
        };
    }

    private void AnnounceFocusedControlIfNeeded()
    {
        var focusOwner = _screen.GetViewport()?.GuiGetFocusOwner();
        if (focusOwner == null)
            return;

        if (ShouldSuppressOpeningCardFocus(focusOwner))
            return;

        var element = GetElement(focusOwner);
        if (element != null)
            UIManager.SetFocusedControl(focusOwner, element);
    }

    private void WireFocusNeighbors()
    {
        var search = Usable(_screen.GetNodeOrNull<NSearchBar>("%SearchBar")?.TextArea);
        var pool = GetUsableRowControls(_poolRow);
        var type = GetUsableRowControls(_typeRow);
        var rarity = GetUsableRowControls(_rarityRow);
        var cost = GetUsableRowControls(_costRow);
        var display = GetUsableRowControls(_displayRow);
        var cards = GetVisibleCardRows();

        if (search != null)
        {
            var self = search.GetPath();
            search.FocusNeighborLeft = self;
            search.FocusNeighborRight = self;
            search.FocusNeighborTop = self;
            search.FocusNeighborBottom = (pool.FirstOrDefault() ?? type.FirstOrDefault() ?? rarity.FirstOrDefault() ?? cost.FirstOrDefault() ?? display.FirstOrDefault() ?? search).GetPath();
        }

        WireHorizontalRow(pool, search != null ? new List<Control> { search } : null, type.Count > 0 ? type : rarity.Count > 0 ? rarity : cost.Count > 0 ? cost : display);
        WireHorizontalRow(type, pool.Count > 0 ? pool : search != null ? new List<Control> { search } : null, rarity.Count > 0 ? rarity : cost.Count > 0 ? cost : display);
        WireHorizontalRow(rarity, type.Count > 0 ? type : pool.Count > 0 ? pool : search != null ? new List<Control> { search } : null, cost.Count > 0 ? cost : display);
        WireHorizontalRow(cost, rarity.Count > 0 ? rarity : type.Count > 0 ? type : pool.Count > 0 ? pool : search != null ? new List<Control> { search } : null, display.Count > 0 ? display : cards.FirstOrDefault()?.Cast<Control>().ToList());
        WireHorizontalRow(display, cost.Count > 0 ? cost : rarity.Count > 0 ? rarity : type.Count > 0 ? type : pool.Count > 0 ? pool : search != null ? new List<Control> { search } : null, cards.FirstOrDefault()?.Cast<Control>().ToList());

        if (cards.Count == 0)
            return;

        var topAnchor = display.FirstOrDefault() ?? cost.FirstOrDefault() ?? rarity.FirstOrDefault() ?? type.FirstOrDefault() ?? pool.FirstOrDefault() ?? search;
        WireCardGridNeighbors(cards, topAnchor);
    }

    private void WireHorizontalRow(List<Control> row, List<Control>? above, List<Control>? below)
    {
        for (int i = 0; i < row.Count; i++)
        {
            var self = row[i].GetPath();
            row[i].FocusNeighborLeft = i > 0 ? row[i - 1].GetPath() : self;
            row[i].FocusNeighborRight = i < row.Count - 1 ? row[i + 1].GetPath() : self;
            row[i].FocusNeighborTop = ResolveVerticalTarget(above, row).GetPath();
            row[i].FocusNeighborBottom = ResolveVerticalTarget(below, row).GetPath();
        }
    }

    private static Control ResolveVerticalTarget(List<Control>? targetRow, List<Control> row)
    {
        if (targetRow != null && targetRow.Count > 0)
            return targetRow[0];

        return row[0];
    }

    private static void WireCardGridNeighbors(List<List<NGridCardHolder>> rows, Control? topAnchor)
    {
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (int colIndex = 0; colIndex < row.Count; colIndex++)
            {
                var holder = row[colIndex];
                holder.FocusNeighborLeft = (colIndex > 0 ? row[colIndex - 1] : holder).GetPath();
                holder.FocusNeighborRight = (colIndex < row.Count - 1 ? row[colIndex + 1] : holder).GetPath();

                if (rowIndex == 0)
                {
                    holder.FocusNeighborTop = (topAnchor ?? holder).GetPath();
                }
                else
                {
                    var previousRow = rows[rowIndex - 1];
                    holder.FocusNeighborTop = (colIndex < previousRow.Count ? previousRow[colIndex] : holder).GetPath();
                }

                if (rowIndex < rows.Count - 1)
                {
                    var nextRow = rows[rowIndex + 1];
                    holder.FocusNeighborBottom = (colIndex < nextRow.Count ? nextRow[colIndex] : holder).GetPath();
                }
                else
                {
                    holder.FocusNeighborBottom = holder.GetPath();
                }
            }
        }
    }

    private List<List<NGridCardHolder>> GetVisibleCardRows()
    {
        var result = new List<List<NGridCardHolder>>();
        var grid = _screen.GetNodeOrNull<NCardLibraryGrid>("%CardGrid");
        var cardRows = grid == null ? null : CardRowsField?.GetValue(grid) as System.Collections.IList;
        if (cardRows == null)
            return result;

        for (int row = 0; row < cardRows.Count; row++)
        {
            var rowList = cardRows[row] as System.Collections.IList;
            if (rowList == null)
                continue;

            var visible = rowList.OfType<NGridCardHolder>().Where(holder => holder.Visible).ToList();
            if (visible.Count > 0)
                result.Add(visible);
        }

        return result;
    }

    private static int GetGridColumnCount(System.Collections.IList cardRows)
    {
        var maxColumns = 0;
        for (int i = 0; i < cardRows.Count; i++)
        {
            if (cardRows[i] is System.Collections.IList rowList && rowList.Count > maxColumns)
                maxColumns = rowList.Count;
        }

        return maxColumns > 0 ? maxColumns : 1;
    }

    private static Control? Usable(Control? control)
    {
        return IsUsable(control) ? control : null;
    }

    private static bool IsUsable(Control? control)
    {
        return control != null && control.Visible && GodotObject.IsInstanceValid(control);
    }

    public bool ShouldSuppressOpeningCardFocus(Control control)
    {
        if (!_suppressOpeningCardFocus || control is not NGridCardHolder)
            return false;

        return true;
    }

    public UIElement? ResolveLiveCardHolder(NGridCardHolder holder)
    {
        if (!IsUsable(holder))
            return null;

        _cardGridContainer.ContainerLabel = GetCardCountLabel();

        var existing = GetElement(holder);
        if (existing != null)
            return existing;

        var proxy = GetOrCreate(holder, () => new ProxyCard(holder));
        _cardGridContainer.Upsert(proxy, holder);
        if (!ReferenceEquals(_cardGridContainer.Parent, _root))
            _root.Add(_cardGridContainer);
        Register(holder, proxy);
        return proxy;
    }

    private List<Control> GetUsableRowControls(ListContainer row)
    {
        return GetRegisteredControls()
            .Where(pair => pair.Value.Parent == row && IsUsable(pair.Key))
            .Select(pair => pair.Key)
            .ToList();
    }

    private string BuildStateToken()
    {
        var parts = new List<string>
        {
            _screen.GetNodeOrNull<NSearchBar>("%SearchBar")?.Text ?? "",
            GetCardCountLabel(),
        };

        foreach (var path in new[]
        {
            "%IroncladPool", "%SilentPool", "%DefectPool", "%RegentPool", "%NecrobinderPool", "%ColorlessPool", "%AncientsPool", "%MiscPool"
        })
        {
            var filter = _screen.GetNodeOrNull<NCardPoolFilter>(path);
            parts.Add($"{path}:{filter?.Visible}:{filter?.IsSelected}");
        }

        foreach (var path in new[]
        {
            "%CardTypeSorter", "%RaritySorter", "%CostSorter", "%AlphabetSorter"
        })
        {
            var sorter = _screen.GetNodeOrNull<NCardViewSortButton>(path);
            parts.Add($"{path}:{sorter?.Visible}:{sorter?.IsDescending}");
        }

        foreach (var path in new[]
        {
            "%AttackType", "%SkillType", "%PowerType", "%OtherType",
            "%CommonRarity", "%UncommonRarity", "%RareRarity", "%OtherRarity",
            "%Cost0", "%Cost1", "%Cost2", "%Cost3+", "%CostX",
            "%Stats", "%Upgrades", "%MultiplayerCards"
        })
        {
            var tickbox = _screen.GetNodeOrNull<Control>(path);
            parts.Add($"{path}:{tickbox?.Visible}:{GetToggleState(tickbox)}");
        }

        var visibleCards = _screen.GetNodeOrNull<NCardLibraryGrid>("%CardGrid")?.VisibleCards.ToList() ?? new List<CardModel>();
        parts.Add(visibleCards.Count.ToString());
        parts.Add(string.Join(",", visibleCards.Select(card => card.Id.ToString())));
        return string.Join("|", parts);
    }

    private static bool? GetToggleState(Control? control)
    {
        return control switch
        {
            NCardPoolFilter pool => pool.IsSelected,
            NTickbox tickbox => tickbox.IsTicked,
            _ => null
        };
    }

    private string GetCardCountLabel()
    {
        var count = _screen.GetNodeOrNull<NCardLibraryGrid>("%CardGrid")?.VisibleCards.Count() ?? 0;
        var loc = new LocString("card_library", "CARD_COUNT");
        loc.Add("Amount", count);
        var text = loc.GetFormattedText();
        if (!string.IsNullOrWhiteSpace(text))
            return text;

        return Ui("CARD_LIBRARY.CARDS");
    }

    private List<string> GetToggleSideEffectAnnouncements(Control? sourceControl)
    {
        var changes = new List<string>();

        foreach (var control in GetTrackedToggleControls())
        {
            var id = control.GetInstanceId();
            var current = GetToggleState(control);
            if (current == null)
                continue;

            _toggleStates.TryGetValue(id, out var previous);
            _toggleStates[id] = current.Value;
            if (current.Value == previous)
                continue;

            if (ReferenceEquals(control, sourceControl))
                continue;

            if (current.Value)
                continue;

            var label = GetElement(control)?.GetLabel();
            var uncheckedText = LocalizationManager.Get("ui", "CHECKBOX.UNCHECKED");
            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(uncheckedText))
                changes.Add($"{label}, {uncheckedText}");
        }

        return changes;
    }

    private void RefreshToggleStateSnapshot()
    {
        _toggleStates.Clear();
        foreach (var control in GetTrackedToggleControls())
        {
            var state = GetToggleState(control);
            if (state != null)
                _toggleStates[control.GetInstanceId()] = state.Value;
        }
    }

    private IEnumerable<Control> GetTrackedToggleControls()
    {
        return new Control?[]
        {
            _screen.GetNodeOrNull<Control>("%IroncladPool"),
            _screen.GetNodeOrNull<Control>("%SilentPool"),
            _screen.GetNodeOrNull<Control>("%DefectPool"),
            _screen.GetNodeOrNull<Control>("%RegentPool"),
            _screen.GetNodeOrNull<Control>("%NecrobinderPool"),
            _screen.GetNodeOrNull<Control>("%ColorlessPool"),
            _screen.GetNodeOrNull<Control>("%AncientsPool"),
            _screen.GetNodeOrNull<Control>("%MiscPool"),
            _screen.GetNodeOrNull<Control>("%AttackType"),
            _screen.GetNodeOrNull<Control>("%SkillType"),
            _screen.GetNodeOrNull<Control>("%PowerType"),
            _screen.GetNodeOrNull<Control>("%OtherType"),
            _screen.GetNodeOrNull<Control>("%CommonRarity"),
            _screen.GetNodeOrNull<Control>("%UncommonRarity"),
            _screen.GetNodeOrNull<Control>("%RareRarity"),
            _screen.GetNodeOrNull<Control>("%OtherRarity"),
            _screen.GetNodeOrNull<Control>("%Cost0"),
            _screen.GetNodeOrNull<Control>("%Cost1"),
            _screen.GetNodeOrNull<Control>("%Cost2"),
            _screen.GetNodeOrNull<Control>("%Cost3+"),
            _screen.GetNodeOrNull<Control>("%CostX"),
            _screen.GetNodeOrNull<Control>("%Stats"),
            _screen.GetNodeOrNull<Control>("%Upgrades"),
            _screen.GetNodeOrNull<Control>("%MultiplayerCards"),
        }.Where(IsUsable)!;
    }

    private static ListContainer NewRow(string label) => new()
    {
        ContainerLabel = label,
        AnnounceName = true,
        AnnouncePosition = true,
    };

    private void AddRowIfNotEmpty(ListContainer row)
    {
        if (row.Children.Count > 0)
            _root.Add(row);
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }

    private bool TryGoBack()
    {
        var backButton = _screen.GetNodeOrNull<NClickableControl>("BackButton");
        if (backButton == null)
            return false;

        backButton.EmitSignal(NClickableControl.SignalName.Released, backButton);
        return true;
    }
}
