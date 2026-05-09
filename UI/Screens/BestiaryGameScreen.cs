using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
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

    private readonly List<NBestiaryEntry> _entryNodes = new();
    /// <summary>First entry of each act, in the same order acts appear in the sidebar.</summary>
    private readonly List<NBestiaryEntry> _actStartEntries = new();
    private readonly List<NBestiaryMoveButton> _moveNodes = new();
    private NBestiaryEntry? _lastSelectedEntry;
    private int _lastMoveCount = -1;

    public override Message? ScreenName => Message.Localized("ui", "SCREENS.BESTIARY");

    public override List<HelpMessage> GetHelpMessages() => new()
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

    public BestiaryGameScreen(NBestiary screen)
    {
        _screen = screen;
        _root.Add(_monsters);
        _root.Add(_actions);
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
            case "mega_view_deck_and_tab_left":
                return JumpToActStart(-1);
            case "mega_view_exhaust_pile_and_tab_right":
                return JumpToActStart(1);
        }

        var index = System.Array.IndexOf(MoveHotkeyActions, action.Key);
        if (index < 0 || index >= _moveNodes.Count)
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
        _monsters.Clear();
        _actions.Clear();
        _entryNodes.Clear();
        _actStartEntries.Clear();
        _moveNodes.Clear();
        _connectedControls.Clear();
        _lastSelectedEntry = null;
        _lastMoveCount = -1;
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
        WireFocusNeighbors();

        _lastSelectedEntry = SelectedEntry;
        _lastMoveCount = MoveListNode?.GetChildCount() ?? 0;
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
            switch (child)
            {
                case NBestiaryActDivider divider:
                    {
                        var label = ProxyElement.FindChildTextPublic(divider) ?? "";
                        currentAct = new ListContainer
                        {
                            ContainerLabel = Message.Raw(label),
                            AnnounceName = true,
                            AnnouncePosition = true,
                        };
                        _monsters.Add(currentAct);
                        actNeedsFirstEntry = true;
                        break;
                    }
                case NBestiaryEntry entry:
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
                        break;
                    }
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
    /// Up/Down on a sidebar entry → previous/next entry, skipping act dividers.
    /// Right on a sidebar entry → topmost move button.
    /// Left on any move button → currently-selected sidebar entry.
    /// All other directions self-loop so the user can't navigate into empty
    /// space and lose focus.
    /// </summary>
    private void WireFocusNeighbors()
    {
        var topMove = _moveNodes.Count > 0 ? _moveNodes[0] : null;
        var topMovePath = topMove?.GetPath();

        for (int i = 0; i < _entryNodes.Count; i++)
        {
            var entry = _entryNodes[i];
            var self = entry.GetPath();
            entry.FocusNeighborTop = i > 0 ? _entryNodes[i - 1].GetPath() : self;
            entry.FocusNeighborBottom = i < _entryNodes.Count - 1
                ? _entryNodes[i + 1].GetPath()
                : self;
            entry.FocusNeighborLeft = self;
            entry.FocusNeighborRight = topMovePath ?? self;
        }

        var selected = SelectedEntry;
        var selectedPath = selected != null && _entryNodes.Contains(selected)
            ? selected.GetPath()
            : (_entryNodes.Count > 0 ? _entryNodes[0].GetPath() : null);

        for (int i = 0; i < _moveNodes.Count; i++)
        {
            var move = _moveNodes[i];
            var self = move.GetPath();
            move.FocusNeighborTop = i > 0 ? _moveNodes[i - 1].GetPath() : self;
            move.FocusNeighborBottom = i < _moveNodes.Count - 1
                ? _moveNodes[i + 1].GetPath()
                : self;
            move.FocusNeighborLeft = selectedPath ?? self;
            move.FocusNeighborRight = self;
        }
    }

    private NBestiaryEntry? SelectedEntry =>
        SelectedEntryField.GetValue(_screen) as NBestiaryEntry;

    private Godot.Container? MoveListNode =>
        MoveListField.GetValue(_screen) as Godot.Container;
}
