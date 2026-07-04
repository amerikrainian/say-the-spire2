using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using SayTheSpire2.Help;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Screen for the settings modding menu (NModdingScreen). Registers a proxy
/// per mod row, announces the pending-changes warning when it appears, and
/// rebuilds when mods are detected at runtime.
/// </summary>
public class ModsGameScreen : GameScreen
{
    private static readonly System.Reflection.FieldInfo ModRowContainerField =
        AccessTools.Field(typeof(NModdingScreen), "_modRowContainer")!;
    private static readonly System.Reflection.FieldInfo PendingWarningField =
        AccessTools.Field(typeof(NModdingScreen), "_pendingChangesWarning")!;

    private readonly NModdingScreen _screen;
    private readonly ListContainer _root;
    private readonly ListContainer _mods = new()
    {
        AnnounceName = true,
        AnnouncePosition = true,
    };

    private readonly List<Control> _rowNodes = new();
    private Control? _getModsButton;
    private Control? _makeModsButton;
    private bool _buttonsRegistered;
    private int _lastRowCount = -1;
    private bool _lastWarningVisible;
    private NModMenuRow? _lastSelectedRow;

    public override Message? ScreenName =>
        Message.Raw(ProxyElement.StripBbcode(
            new LocString("settings_ui", "MODDING_SCREEN_BUTTON_LABEL").GetFormattedText()));

    public ModsGameScreen(NModdingScreen screen)
    {
        _screen = screen;
        _root = new ListContainer
        {
            ContainerLabel = ScreenName,
            AnnounceName = true,
            AnnouncePosition = false,
        };
        _mods.ContainerLabel = Message.Raw(ProxyElement.StripBbcode(
            new LocString("settings_ui", "MODDING_SCREEN.INSTALLED_MODS_TITLE").GetFormattedText()));
        _root.Add(_mods);
        RootElement = _root;
    }

    public override List<HelpMessage> GetHelpMessages() => new()
    {
        new TextHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MODS",
            "Press confirm on a mod to show its details. Press confirm again to enable or disable it. Changes take effect after restarting the game."), exclusive: true),
    };

    protected override void BuildRegistry()
    {
        BuildRows();
        _lastWarningVisible = IsWarningVisible;
        _lastSelectedRow = SelectedRow;
    }

    public override void OnPop()
    {
        base.OnPop();
        _mods.Clear();
        _rowNodes.Clear();
        _connectedControls.Clear();
        _getModsButton = null;
        _makeModsButton = null;
        _buttonsRegistered = false;
        _lastRowCount = -1;
    }

    public override void OnUpdate()
    {
        // Mods can be detected at runtime (e.g. a workshop download finishing
        // while the screen is open); pick up new rows.
        var rowCount = RowContainer?.GetChildCount() ?? 0;
        if (rowCount != _lastRowCount)
            BuildRows();

        // Selecting a mod (first confirm on its row) fills the info panel;
        // announce "selected" followed by the same details.
        var selectedRow = SelectedRow;
        if (!ReferenceEquals(selectedRow, _lastSelectedRow))
        {
            _lastSelectedRow = selectedRow;
            if (selectedRow != null)
                AnnounceSelection(selectedRow);
        }

        // The pending-changes warning is the only "restart required" signal.
        // Announce it with the game's own wording when it appears.
        var warningVisible = IsWarningVisible;
        if (warningVisible && !_lastWarningVisible)
        {
            SpeechManager.Output(Message.Raw(ProxyElement.StripBbcode(
                new LocString("settings_ui", "MODDING_SCREEN.PENDING_CHANGES_WARNING").GetFormattedText())));
        }
        _lastWarningVisible = warningVisible;
    }

    private void BuildRows()
    {
        _mods.Clear();
        _rowNodes.Clear();

        var container = RowContainer;
        if (container == null)
        {
            _lastRowCount = -1;
            return;
        }

        _lastRowCount = container.GetChildCount();
        foreach (var row in container.GetChildren().OfType<NModMenuRow>())
        {
            var proxy = new ProxyModMenuRow(row);
            _mods.Add(proxy);
            Register(row, proxy);
            _rowNodes.Add(row);
        }

        _getModsButton ??= _screen.GetNodeOrNull<Control>("%GetModsButton");
        _makeModsButton ??= _screen.GetNodeOrNull<Control>("%MakeModsButton");
        if (!_buttonsRegistered)
        {
            foreach (var button in new[] { _getModsButton, _makeModsButton })
            {
                if (button == null) continue;
                var proxy = ProxyFactory.Create(button);
                _root.Add(proxy);
                Register(button, proxy);
                _buttonsRegistered = true;
            }
        }

        WireFocusNeighbors();
    }

    /// <summary>
    /// The game's own controller wiring here is unreliable and never reaches
    /// the two link buttons, so we wire one vertical chain ourselves: every
    /// mod row top to bottom, then Get Mods, then Make Mods. Edges self-loop.
    /// </summary>
    private void WireFocusNeighbors()
    {
        var chain = new List<Control>();
        foreach (var row in _rowNodes)
        {
            if (GodotObject.IsInstanceValid(row))
                chain.Add(row);
        }
        foreach (var button in new[] { _getModsButton, _makeModsButton })
        {
            if (button != null && GodotObject.IsInstanceValid(button))
                chain.Add(button);
        }

        for (int i = 0; i < chain.Count; i++)
        {
            var control = chain[i];
            // Buttons ship mouse-only (FocusMode.None); force them navigable.
            control.FocusMode = Control.FocusModeEnum.All;
            var self = control.GetPath();
            control.FocusNeighborTop = i > 0 ? chain[i - 1].GetPath() : self;
            control.FocusNeighborBottom = i < chain.Count - 1 ? chain[i + 1].GetPath() : self;
            control.FocusNeighborLeft = self;
            control.FocusNeighborRight = self;
        }
    }

    private void AnnounceSelection(NModMenuRow row)
    {
        var parts = new List<Message> { Message.Localized("ui", "CARD.SELECTED") };
        if (GetElement(row) is ProxyModMenuRow proxy)
        {
            foreach (var item in proxy.GetDetailItems())
                parts.Add(Message.Raw(item));
        }
        SpeechManager.Output(Message.Join(", ", parts.ToArray()));
    }

    private NModMenuRow? SelectedRow =>
        _rowNodes.OfType<NModMenuRow>().FirstOrDefault(r =>
            GodotObject.IsInstanceValid(r) && ProxyModMenuRow.IsRowSelected(r));

    private Control? RowContainer => ModRowContainerField.GetValue(_screen) as Control;

    private bool IsWarningVisible =>
        PendingWarningField.GetValue(_screen) is Control warning
        && GodotObject.IsInstanceValid(warning)
        && warning.Visible;
}
