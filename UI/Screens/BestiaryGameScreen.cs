using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class BestiaryGameScreen : GameScreen
{
    /// <summary>
    /// `mega_select_card_1..9` are the same hotkeys the game's
    /// NBestiaryMoveButton listens for, but its _UnhandledInput skips them in
    /// controller mode — which we force on for accessibility — so we forward
    /// them to the matching move button ourselves.
    /// </summary>
    private static readonly string[] MoveHotkeyActions =
    {
        "mega_select_card_1",
        "mega_select_card_2",
        "mega_select_card_3",
        "mega_select_card_4",
        "mega_select_card_5",
        "mega_select_card_6",
        "mega_select_card_7",
        "mega_select_card_8",
        "mega_select_card_9",
    };

    private static readonly System.Reflection.FieldInfo BestiaryListField =
        AccessTools.Field(typeof(NBestiary), "_bestiaryList")!;
    private static readonly System.Reflection.FieldInfo MoveListField =
        AccessTools.Field(typeof(NBestiary), "_moveList")!;
    private static readonly System.Reflection.FieldInfo SelectedEntryField =
        AccessTools.Field(typeof(NBestiary), "_selectedEntry")!;

    // The stats view (mode toggle + character filters) exists only on the
    // beta branch; these handles are null on stable and the whole feature is
    // gated on HasStatsSupport.
    private static readonly System.Reflection.FieldInfo? ModeButtonField =
        AccessTools.Field(typeof(NBestiary), "_modeButton");
    private static readonly System.Reflection.FieldInfo? FilterContainerField =
        AccessTools.Field(typeof(NBestiary), "_filterContainer");
    private static readonly System.Reflection.FieldInfo? IsStatsModeField =
        AccessTools.Field(typeof(NBestiary), "_isStatsMode");
    private static readonly System.Reflection.FieldInfo? CurrentFilterField =
        AccessTools.Field(typeof(NBestiary), "_currentFilter");

    private static bool HasStatsSupport =>
        ModeButtonField != null && FilterContainerField != null && IsStatsModeField != null;

    private readonly NBestiary _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = Message.Localized("ui", "SCREENS.BESTIARY"),
        AnnounceName = true,
        AnnouncePosition = false,
    };
    private readonly ListContainer _monsters = new()
    {
        ContainerLabel = Message.Localized("ui", "BESTIARY.MONSTERS"),
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly ListContainer _actions = new()
    {
        ContainerLabel = Message.Localized("ui", "BESTIARY.ACTIONS"),
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly ListContainer _filters = new()
    {
        ContainerLabel = Message.Localized("ui", "BESTIARY.FILTERS"),
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly ListContainer _stats = new()
    {
        ContainerLabel = Message.Localized("ui", "BESTIARY.STATS"),
        AnnounceName = true,
        AnnouncePosition = true,
    };

    /// <summary>Stat lines in STATS.layout (encounters/kills/deaths/win rate); one focusable node each.</summary>
    private const int StatLineCount = 4;

    private readonly List<NBestiaryEntry> _entryNodes = new();
    /// <summary>First entry of each act, in the same order acts appear in the sidebar.</summary>
    private readonly List<NBestiaryEntry> _actStartEntries = new();
    private readonly List<NBestiaryMoveButton> _moveNodes = new();
    private readonly List<Control> _filterNodes = new();
    /// <summary>Mod-created invisible focusable nodes: one per stat line, plus the quote last.</summary>
    private readonly List<Control> _statNodes = new();
    /// <summary>Proxies for _statNodes, index-aligned.</summary>
    private readonly List<ProxyBestiaryStat> _statProxies = new();
    private Control? _modeButtonNode;
    private ProxyBestiaryModeButton? _modeProxy;
    private NBestiaryEntry? _lastSelectedEntry;
    private int _lastMoveCount = -1;
    private bool _lastStatsMode;
    private object? _lastFilter;

    public override Message? ScreenName => Message.Localized("ui", "SCREENS.BESTIARY");

    public override List<HelpMessage> GetHelpMessages()
    {
        var messages = new List<HelpMessage>
        {
            new TextHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.BESTIARY_NAV",
                "Up and down move through the monster list. Right enters the actions list for the focused monster; left from any action returns to the monster."), exclusive: true),
            new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.BESTIARY_PREV_ACT", "Previous Act"),
                "mega_view_deck_and_tab_left", exclusive: true),
            new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.BESTIARY_NEXT_ACT", "Next Act"),
                "mega_view_exhaust_pile_and_tab_right", exclusive: true),
            new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.BESTIARY_PLAY_MOVE", "Play Move 1-9"),
                MoveHotkeyActions, exclusive: true),
        };
        if (HasStatsSupport)
        {
            messages.Add(new TextHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.BESTIARY_STATS",
                "The view button switches between actions and stats. In the stats view, the previous and next act keys switch the character filter instead."), exclusive: true));
        }
        return messages;
    }

    public BestiaryGameScreen(NBestiary screen)
    {
        _screen = screen;
        _root.Add(_monsters);
        _root.Add(_actions);
        if (HasStatsSupport && ModeButtonField!.GetValue(screen) is Control modeButton)
        {
            _modeButtonNode = modeButton;
            _modeProxy = new ProxyBestiaryModeButton(modeButton);
            _root.Add(_modeProxy);
            _root.Add(_filters);
            _root.Add(_stats);
        }
        RootElement = _root;

        foreach (var action in MoveHotkeyActions)
            ClaimAction(action);

        ClaimAction("mega_view_deck_and_tab_left");
        ClaimAction("mega_view_exhaust_pile_and_tab_right");
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            // In the stats view the game rebinds these keys to character
            // filter paging via NHotkeyManager (which sees the input
            // regardless of our claim), so stay out of its way.
            case "mega_view_deck_and_tab_left":
                return !IsStatsMode && JumpToActStart(-1);
            case "mega_view_exhaust_pile_and_tab_right":
                return !IsStatsMode && JumpToActStart(1);
        }

        var index = System.Array.IndexOf(MoveHotkeyActions, action.Key);
        if (index < 0 || index >= _moveNodes.Count)
            return false;
        // The game disables move hotkeys in the stats view (the move panel is
        // hidden); mirror that so numbers don't trigger invisible moves.
        if (IsStatsMode)
            return false;
        Activate(_moveNodes[index]);
        return true;
    }

    /// <summary>
    /// Tab-left/right navigation between act starts. When tabbing left from
    /// somewhere mid-act, jump to the start of the current act first; another
    /// press jumps to the previous act. Tab-right always jumps to the next
    /// act's first entry. No-op when the focused control is not a sidebar
    /// entry, or when there's nowhere to go (boundary).
    /// </summary>
    private bool JumpToActStart(int direction)
    {
        if (_actStartEntries.Count == 0) return false;

        var focused = FindFocusedEntry();
        if (focused == null) return false;

        int currentActIdx = _actStartEntries.IndexOf(focused);

        NBestiaryEntry? target;
        if (direction < 0)
        {
            if (currentActIdx < 0)
            {
                // Mid-act: jump to start of current act
                target = FindCurrentActStart(focused);
            }
            else if (currentActIdx > 0)
            {
                target = _actStartEntries[currentActIdx - 1];
            }
            else
            {
                return false; // already at first act's first entry
            }
        }
        else
        {
            // direction > 0 — find the next act start strictly after `focused`
            int focusedListIdx = _entryNodes.IndexOf(focused);
            target = _actStartEntries
                .FirstOrDefault(e => _entryNodes.IndexOf(e) > focusedListIdx);
        }

        if (target == null || !GodotObject.IsInstanceValid(target))
            return false;
        target.GrabFocus();
        return true;
    }

    private NBestiaryEntry? FindFocusedEntry()
    {
        foreach (var entry in _entryNodes)
            if (GodotObject.IsInstanceValid(entry) && entry.HasFocus())
                return entry;
        return null;
    }

    /// <summary>
    /// Returns the act-start entry whose act contains <paramref name="entry"/>,
    /// or null if nothing precedes it.
    /// </summary>
    private NBestiaryEntry? FindCurrentActStart(NBestiaryEntry entry)
    {
        int focusedIdx = _entryNodes.IndexOf(entry);
        NBestiaryEntry? result = null;
        foreach (var actStart in _actStartEntries)
        {
            int startIdx = _entryNodes.IndexOf(actStart);
            if (startIdx <= focusedIdx)
                result = actStart;
            else
                break;
        }
        return result;
    }

    public override void OnPop()
    {
        base.OnPop();
        FreeStatNodes();
        _monsters.Clear();
        _actions.Clear();
        _filters.Clear();
        _stats.Clear();
        _entryNodes.Clear();
        _actStartEntries.Clear();
        _moveNodes.Clear();
        _filterNodes.Clear();
        _connectedControls.Clear();
        _lastSelectedEntry = null;
        _lastMoveCount = -1;
        _lastFilter = null;
    }

    public override void OnUpdate()
    {
        var selected = SelectedEntry;
        var moveCount = MoveListNode?.GetChildCount() ?? 0;

        if (!ReferenceEquals(_lastSelectedEntry, selected) || moveCount != _lastMoveCount)
        {
            _lastSelectedEntry = selected;
            _lastMoveCount = moveCount;
            BuildActions();
            WireFocusNeighbors();
        }

        if (_modeButtonNode == null)
            return;

        // Announce actions/stats mode changes and rewire the panel neighbors
        // (the reachable panel below the toggle depends on the mode).
        var statsMode = IsStatsMode;
        if (statsMode != _lastStatsMode)
        {
            _lastStatsMode = statsMode;
            WireFocusNeighbors();
            SpeechManager.Output(Message.Localized("ui",
                statsMode ? "SPEECH.BESTIARY_MODE_STATS" : "SPEECH.BESTIARY_MODE_ACTIONS"));
        }

        // Announce character filter changes. This covers clicking a filter as
        // well as the game's own paging hotkeys, which never move focus. The
        // stat chain is rewired because the quote's presence depends on the
        // selected filter.
        var filter = CurrentFilterField?.GetValue(_screen);
        if (!ReferenceEquals(filter, _lastFilter))
        {
            var announce = _lastFilter != null;
            _lastFilter = filter;
            WireFocusNeighbors();
            if (announce && statsMode && filter != null)
                SpeechManager.Output(ProxyBestiaryCharacterFilter.GetFilterName(filter));
        }
    }

    protected override void BuildRegistry()
    {
        _monsters.Clear();
        _actions.Clear();
        _entryNodes.Clear();
        _actStartEntries.Clear();
        _moveNodes.Clear();

        BuildSidebar();
        BuildActions();
        BuildStatsControls();
        WireFocusNeighbors();

        _lastSelectedEntry = SelectedEntry;
        _lastMoveCount = MoveListNode?.GetChildCount() ?? 0;
        _lastStatsMode = IsStatsMode;
        _lastFilter = CurrentFilterField?.GetValue(_screen);
    }

    /// <summary>
    /// Walks the game's bestiary VBoxContainer, grouping entries under their
    /// preceding act divider. Act dividers themselves stay outside the
    /// navigable list — their text is announced via the per-act sub-container's
    /// label when the user enters that group.
    /// </summary>
    private void BuildSidebar()
    {
        var listNode = (Godot.Container?)BestiaryListField.GetValue(_screen);
        if (listNode == null) return;

        ListContainer? currentAct = null;
        bool actNeedsFirstEntry = false;

        foreach (var child in listNode.GetChildren().OfType<Control>())
        {
            if (child is NBestiaryEntry entry)
            {
                var proxy = new ProxyBestiaryEntry(entry);
                (currentAct ?? _monsters).Add(proxy);
                Register(entry, proxy);
                _entryNodes.Add(entry);
                if (actNeedsFirstEntry)
                {
                    _actStartEntries.Add(entry);
                    actNeedsFirstEntry = false;
                }
            }
            // Divider type is branch-divergent (beta: NBestiaryLabelDivider,
            // older builds: NBestiaryActDivider; stable: absent), so match by
            // type name instead of a compile-time reference.
            else if (child.GetType().Name is "NBestiaryLabelDivider" or "NBestiaryActDivider")
            {
                var label = ProxyElement.FindChildTextPublic(child) ?? "";
                currentAct = new ListContainer
                {
                    ContainerLabel = Message.Raw(label),
                    AnnounceName = true,
                    AnnouncePosition = true,
                };
                _monsters.Add(currentAct);
                actNeedsFirstEntry = true;
            }
        }
    }

    private void BuildActions()
    {
        _actions.Clear();
        _moveNodes.Clear();

        var moveList = MoveListNode;
        if (moveList == null) return;

        foreach (var move in moveList.GetChildren().OfType<NBestiaryMoveButton>())
        {
            var proxy = new ProxyBestiaryMoveButton(move);
            _actions.Add(proxy);
            Register(move, proxy);
            _moveNodes.Add(move);
        }
    }

    /// <summary>
    /// Registers the mode toggle button and one proxy per character filter.
    /// No-op on stable (no stats view). The early return also keeps the
    /// beta-only proxy classes from being touched there.
    /// </summary>
    private void BuildStatsControls()
    {
        _filters.Clear();
        _filterNodes.Clear();

        if (_modeButtonNode == null || _modeProxy == null) return;
        Register(_modeButtonNode, _modeProxy);

        if (FilterContainerField?.GetValue(_screen) is not Godot.Container container)
            return;

        foreach (var child in container.GetChildren().OfType<Control>())
        {
            if (!ProxyBestiaryCharacterFilter.IsFilter(child)) continue;
            var proxy = new ProxyBestiaryCharacterFilter(child);
            _filters.Add(proxy);
            Register(child, proxy);
            _filterNodes.Add(child);
        }

        BuildStatNodes();
    }

    /// <summary>
    /// The game draws all stats in one rich-text label, so there is nothing
    /// focusable per stat. Create one invisible zero-size focusable Control
    /// per stat line (plus one for the character's quote) below the filter
    /// row; each announces its line for the currently selected filter.
    /// </summary>
    private void BuildStatNodes()
    {
        FreeStatNodes();
        _stats.Clear();

        for (int i = 0; i <= StatLineCount; i++)
        {
            bool isQuote = i == StatLineCount;
            var node = new Control
            {
                Name = isQuote ? "AccessBestiaryQuote" : $"AccessBestiaryStat{i}",
                FocusMode = Control.FocusModeEnum.All,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            _screen.AddChild(node);
            var proxy = new ProxyBestiaryStat(node, i, isQuote);
            Register(node, proxy);
            ConnectFocusSignal(node, proxy);
            _statNodes.Add(node);
            _statProxies.Add(proxy);
        }
    }

    private void FreeStatNodes()
    {
        foreach (var node in _statNodes)
        {
            if (GodotObject.IsInstanceValid(node))
                node.QueueFree();
        }
        _statNodes.Clear();
        _statProxies.Clear();
    }

    /// <summary>
    /// Up/Down on a sidebar entry → previous/next entry, skipping act dividers.
    /// Right on a sidebar entry → the mode toggle at the top of the detail
    /// panel (beta), or the topmost move button (stable).
    /// Down from the toggle → first move button (actions view) or first
    /// character filter (stats view). Filters run left/right as a row.
    /// Left from the panel returns to the currently-selected sidebar entry.
    /// All other directions self-loop so the user can't navigate into empty
    /// space and lose focus.
    /// </summary>
    private void WireFocusNeighbors()
    {
        var statsMode = IsStatsMode;
        var modeButton = _modeButtonNode != null && GodotObject.IsInstanceValid(_modeButtonNode)
            ? _modeButtonNode
            : null;

        // The mode button and character filters ship with FocusMode.None in
        // the scene (mouse-only for sighted players; controller users page
        // filters via hotkeys), so Godot refuses to move focus onto them.
        // Force them navigable — re-asserted every rewire because the game's
        // Enable() resets FocusMode to the scene default.
        if (modeButton != null)
            modeButton.FocusMode = Control.FocusModeEnum.All;
        foreach (var filterNode in _filterNodes)
        {
            if (GodotObject.IsInstanceValid(filterNode))
                filterNode.FocusMode = Control.FocusModeEnum.All;
        }

        var modePath = modeButton?.GetPath();
        var topMove = _moveNodes.Count > 0 ? _moveNodes[0] : null;
        var topMovePath = topMove?.GetPath();
        var panelTopPath = modePath ?? topMovePath;

        for (int i = 0; i < _entryNodes.Count; i++)
        {
            var entry = _entryNodes[i];
            var self = entry.GetPath();
            entry.FocusNeighborTop = i > 0 ? _entryNodes[i - 1].GetPath() : self;
            entry.FocusNeighborBottom = i < _entryNodes.Count - 1
                ? _entryNodes[i + 1].GetPath()
                : self;
            entry.FocusNeighborLeft = self;
            entry.FocusNeighborRight = panelTopPath ?? self;
        }

        var selected = SelectedEntry;
        var selectedPath = selected != null && _entryNodes.Contains(selected)
            ? selected.GetPath()
            : (_entryNodes.Count > 0 ? _entryNodes[0].GetPath() : null);

        if (modeButton != null)
        {
            var self = modeButton.GetPath();
            var below = statsMode
                ? (_filterNodes.Count > 0 ? _filterNodes[0].GetPath() : null)
                : topMovePath;
            modeButton.FocusNeighborTop = self;
            modeButton.FocusNeighborBottom = below ?? self;
            modeButton.FocusNeighborLeft = selectedPath ?? self;
            modeButton.FocusNeighborRight = self;
        }

        // The reachable stat chain: the stat lines the layout actually
        // produces, plus the quote node only when the selected filter has a
        // quote. The _stats container is rebuilt to match so announced
        // positions ("2 of 4") reflect what is really navigable.
        var currentFilter = CurrentFilterField?.GetValue(_screen);
        var statChain = new List<Control>();
        _stats.Clear();
        if (statsMode && currentFilter != null)
        {
            int lineCount = System.Math.Min(
                ProxyBestiaryCharacterFilter.GetStatLines(currentFilter).Count(),
                StatLineCount);
            bool hasQuote = ProxyBestiaryCharacterFilter.GetQuote(currentFilter) != null;
            for (int i = 0; i < _statNodes.Count; i++)
            {
                var node = _statNodes[i];
                if (!GodotObject.IsInstanceValid(node)) continue;
                bool isQuote = i == StatLineCount;
                if (isQuote ? !hasQuote : i >= lineCount) continue;
                statChain.Add(node);
                _stats.Add(_statProxies[i]);
            }
        }
        var firstStatPath = statChain.Count > 0 ? statChain[0].GetPath() : (NodePath?)null;

        var selectedFilter = _filterNodes.FirstOrDefault(f =>
            GodotObject.IsInstanceValid(f) && ProxyBestiaryCharacterFilter.IsSelectedFilter(f));
        var filterReturnPath = (selectedFilter ?? _filterNodes.FirstOrDefault())?.GetPath();

        for (int i = 0; i < _filterNodes.Count; i++)
        {
            var filter = _filterNodes[i];
            var self = filter.GetPath();
            filter.FocusNeighborTop = modePath ?? self;
            filter.FocusNeighborBottom = firstStatPath ?? self;
            filter.FocusNeighborLeft = i > 0
                ? _filterNodes[i - 1].GetPath()
                : (selectedPath ?? self);
            filter.FocusNeighborRight = i < _filterNodes.Count - 1
                ? _filterNodes[i + 1].GetPath()
                : self;
        }

        // Unreachable stat nodes (actions view, or the quote when absent)
        // self-loop everywhere so no stale neighbor can land on them.
        foreach (var node in _statNodes)
        {
            if (!GodotObject.IsInstanceValid(node)) continue;
            var self = node.GetPath();
            node.FocusNeighborTop = self;
            node.FocusNeighborBottom = self;
            node.FocusNeighborLeft = self;
            node.FocusNeighborRight = self;
        }
        for (int i = 0; i < statChain.Count; i++)
        {
            var node = statChain[i];
            var self = node.GetPath();
            node.FocusNeighborTop = i > 0 ? statChain[i - 1].GetPath() : (filterReturnPath ?? self);
            node.FocusNeighborBottom = i < statChain.Count - 1 ? statChain[i + 1].GetPath() : self;
            node.FocusNeighborLeft = selectedPath ?? self;
            node.FocusNeighborRight = self;
        }

        // If focus sits on a stat node that just left the chain (view toggled
        // to actions, or the quote disappeared), rescue it back to the filter
        // row / selected monster instead of stranding it on a self-loop.
        foreach (var node in _statNodes)
        {
            if (!GodotObject.IsInstanceValid(node) || !node.HasFocus() || statChain.Contains(node))
                continue;
            var rescue = statsMode ? (filterReturnPath ?? selectedPath) : selectedPath;
            if (rescue != null && _screen.GetNodeOrNull<Control>(rescue) is Control target)
                target.GrabFocus();
            break;
        }

        for (int i = 0; i < _moveNodes.Count; i++)
        {
            var move = _moveNodes[i];
            var self = move.GetPath();
            move.FocusNeighborTop = i > 0 ? _moveNodes[i - 1].GetPath() : (modePath ?? self);
            move.FocusNeighborBottom = i < _moveNodes.Count - 1
                ? _moveNodes[i + 1].GetPath()
                : self;
            move.FocusNeighborLeft = selectedPath ?? self;
            move.FocusNeighborRight = self;
        }
    }

    private bool IsStatsMode => IsStatsModeField?.GetValue(_screen) is true;

    private NBestiaryEntry? SelectedEntry =>
        SelectedEntryField.GetValue(_screen) as NBestiaryEntry;

    private Godot.Container? MoveListNode =>
        MoveListField.GetValue(_screen) as Godot.Container;
}
