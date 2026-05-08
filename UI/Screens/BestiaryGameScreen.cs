using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
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
    private readonly List<NBestiaryMoveButton> _moveNodes = new();
    private NBestiaryEntry? _lastSelectedEntry;
    private int _lastMoveCount = -1;

    public override Message? ScreenName => Message.Localized("ui", "SCREENS.BESTIARY");

    public BestiaryGameScreen(NBestiary screen)
    {
        _screen = screen;
        _root.Add(_monsters);
        _root.Add(_actions);
        RootElement = _root;

        foreach (var action in MoveHotkeyActions)
            ClaimAction(action);
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        var index = System.Array.IndexOf(MoveHotkeyActions, action.Key);
        if (index < 0 || index >= _moveNodes.Count)
            return false;
        Activate(_moveNodes[index]);
        return true;
    }

    public override void OnPop()
    {
        base.OnPop();
        _monsters.Clear();
        _actions.Clear();
        _entryNodes.Clear();
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
                        break;
                    }
                case NBestiaryEntry entry:
                    {
                        var proxy = new ProxyBestiaryEntry(entry);
                        (currentAct ?? _monsters).Add(proxy);
                        Register(entry, proxy);
                        _entryNodes.Add(entry);
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
