using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CustomRunGameScreen : GameScreen
{
    public static CustomRunGameScreen? Current { get; private set; }

    private readonly NCustomRunScreen _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = new LocString("main_menu_ui", "CUSTOM_RUN_SCREEN.CUSTOM_MODE_TITLE").GetFormattedText(),
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly HashSet<ulong> _connectedControls = new();
    private readonly Dictionary<ulong, UIElement> _elementCache = new();

    private string? _stateToken;
    private bool _isMultiplayer;
    private int _lastAscension = -1;
    private NCharacterSelectButton? _lastFocusedCharacterButton;
    private Control? _lastFocusedControl;

    private LineEdit? _seedInput;
    private Control? _characterButtonContainer;
    private List<NCharacterSelectButton> _characterButtons = new();
    private NAscensionPanel? _ascensionPanel;
    private NClickableControl? _ascensionLeftArrow;
    private NClickableControl? _ascensionRightArrow;
    private NCustomRunModifiersList? _modifiersList;
    private List<NRunModifierTickbox> _modifierTickboxes = new();
    private NClickableControl? _confirmButton;
    private NClickableControl? _unreadyButton;
    private NClickableControl? _backButton;

    public override string? ScreenName => new LocString("main_menu_ui", "CUSTOM_RUN_SCREEN.CUSTOM_MODE_TITLE").GetFormattedText();
    public override IEnumerable<string> AlwaysEnabledBuffers =>
        _isMultiplayer ? new[] { "lobby" } : Array.Empty<string>();

    public CustomRunGameScreen(NCustomRunScreen screen)
    {
        _screen = screen;
        RootElement = _root;
        ClaimAction("ui_cancel");
        ClaimAction("mega_pause_and_back");
    }

    public override void OnPush()
    {
        Current = this;
        ResolveControls();
        _isMultiplayer = _screen.Lobby != null && _screen.Lobby.NetService.Type != NetGameType.Singleplayer;
        BindLobbyBuffer();
        _stateToken = BuildStateToken();
        base.OnPush();
    }

    public override void OnPop()
    {
        base.OnPop();
        UnbindLobbyBuffer();
        if (Current == this)
            Current = null;
    }

    public override void OnUpdate()
    {
        if (!GodotObject.IsInstanceValid(_screen))
            return;

        ResolveControls();
        PollAscension();

        var token = BuildStateToken();
        if (token != _stateToken)
        {
            _stateToken = token;
            ClearRegistry();
            BuildRegistry();
        }

        EnsureFocus();
    }

    public void OnLobbyUpdated()
    {
        UpdateLobbyBuffer();
        _stateToken = null;
    }

    public void OnStateChanged()
    {
        UpdateLobbyBuffer();
        _stateToken = null;
    }

    public override bool OnActionJustPressed(Input.InputAction action)
    {
        switch (action.Key)
        {
            case "ui_cancel":
            case "mega_pause_and_back":
                Activate(_backButton);
                return true;
            default:
                return false;
        }
    }

    protected override void BuildRegistry()
    {
        _root.Clear();

        if (_seedInput != null)
            RegisterMain(_seedInput, GetOrCreate(_seedInput, () => ProxyFactory.Create(_seedInput)));

        foreach (var button in _characterButtons.Where(IsUsable))
            RegisterMain(button, GetOrCreate(button, () => new ProxyCharacterButton(button)));

        if (_ascensionPanel != null && IsUsable(_ascensionPanel))
        {
            var ascensionElement = GetOrCreate(_ascensionPanel, () => new ActionElement(
                () => Ui("CUSTOM_RUN.ASCENSION", new { value = _ascensionPanel?.Ascension ?? 0 }),
                status: GetAscensionStatus));
            RegisterMain(_ascensionPanel, ascensionElement);
            RegisterAlias(_ascensionLeftArrow, ascensionElement);
            RegisterAlias(_ascensionRightArrow, ascensionElement);
        }

        foreach (var tickbox in _modifierTickboxes.Where(IsUsable))
            RegisterMain(tickbox, GetOrCreate(tickbox, () => ProxyFactory.Create(tickbox)));

        if (_confirmButton != null && IsUsable(_confirmButton))
            RegisterMain(_confirmButton, GetOrCreate(_confirmButton, () => ProxyFactory.Create(_confirmButton)));
        if (_isMultiplayer && _unreadyButton != null && IsUsable(_unreadyButton))
            RegisterMain(_unreadyButton, GetOrCreate(_unreadyButton, () => ProxyFactory.Create(_unreadyButton)));
        if (_backButton != null && IsUsable(_backButton))
            RegisterMain(_backButton, GetOrCreate(_backButton, () => ProxyFactory.Create(_backButton)));

        WireFocusNeighbors();
    }

    private void ResolveControls()
    {
        _seedInput ??= _screen.GetNodeOrNull<LineEdit>("%SeedInput");
        _characterButtonContainer ??= _screen.GetNodeOrNull<Control>("LeftContainer/CharSelectButtons/ButtonContainer");
        if (_characterButtonContainer != null)
            _characterButtons = _characterButtonContainer.GetChildren().OfType<NCharacterSelectButton>().ToList();
        _ascensionPanel ??= _screen.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        _ascensionLeftArrow ??= _ascensionPanel?.GetNodeOrNull<NClickableControl>("HBoxContainer/LeftArrowContainer/LeftArrow");
        _ascensionRightArrow ??= _ascensionPanel?.GetNodeOrNull<NClickableControl>("HBoxContainer/RightArrowContainer/RightArrow");
        _modifiersList ??= _screen.GetNodeOrNull<NCustomRunModifiersList>("%ModifiersList");
        if (_modifiersList != null)
        {
            var content = _modifiersList.GetNodeOrNull<Control>("ScrollContainer/Mask/Content");
            if (content != null)
                _modifierTickboxes = content.GetChildren().OfType<NRunModifierTickbox>().ToList();
        }
        _confirmButton ??= _screen.GetNodeOrNull<NClickableControl>("ConfirmButton");
        _unreadyButton ??= _screen.GetNodeOrNull<NClickableControl>("UnreadyButton");
        _backButton ??= _screen.GetNodeOrNull<NClickableControl>("BackButton");
    }

    private void BindLobbyBuffer()
    {
        if (!_isMultiplayer)
            return;

        if (BufferManager.Instance.GetBuffer("lobby") is not LobbyBuffer buffer || _screen.Lobby == null)
            return;

        buffer.Bind(_screen.Lobby);
        buffer.Update();
        BufferManager.Instance.EnableBuffer("lobby", true);
    }

    private void UnbindLobbyBuffer()
    {
        if (!_isMultiplayer)
            return;

        if (BufferManager.Instance.GetBuffer("lobby") is LobbyBuffer buffer)
            buffer.Enabled = false;
    }

    private void UpdateLobbyBuffer()
    {
        if (!_isMultiplayer)
            return;

        if (BufferManager.Instance.GetBuffer("lobby") is LobbyBuffer buffer)
            buffer.Update();
    }

    private void PollAscension()
    {
        if (_ascensionPanel == null || !_ascensionPanel.Visible)
            return;

        var focusedControl = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        var current = _ascensionPanel.Ascension;
        if (_lastAscension == -1 || focusedControl != _lastFocusedControl)
        {
            _lastAscension = current;
            _lastFocusedControl = focusedControl;
            return;
        }

        if (current == _lastAscension)
        {
            _lastFocusedControl = focusedControl;
            return;
        }

        _lastAscension = current;
        _lastFocusedControl = focusedControl;
        var title = AscensionHelper.GetTitle(current).GetFormattedText();
        var description = AscensionHelper.GetDescription(current).GetFormattedText();
        SpeechManager.Output(Message.Raw($"{Ui("CUSTOM_RUN.ASCENSION", new { value = current })}: {title}. {description}"));
        _stateToken = null;
    }

    private void RegisterMain(Control control, UIElement element)
    {
        control.FocusMode = Control.FocusModeEnum.All;
        Register(control, element);
        ConnectFocusSignal(control, element);
        _root.Add(element);
    }

    private void RegisterAlias(Control? control, UIElement element)
    {
        if (control == null || !GodotObject.IsInstanceValid(control))
            return;

        control.FocusMode = Control.FocusModeEnum.All;
        Register(control, element);
        ConnectFocusSignal(control, element);
    }

    private void ConnectFocusSignal(Control control, UIElement element)
    {
        if (!_connectedControls.Add(control.GetInstanceId()))
            return;

        control.FocusEntered += () =>
        {
            if (control is NCharacterSelectButton button)
                _lastFocusedCharacterButton = button;
            UIManager.SetFocusedControl(control, element);
        };
    }

    private void WireFocusNeighbors()
    {
        var seed = IsUsable(_seedInput) ? _seedInput : null;
        var chars = _characterButtons.Where(IsUsable).Cast<Control>().ToList();
        var ascension = IsUsable(_ascensionPanel) ? _ascensionPanel : null;
        var modifiers = _modifierTickboxes.Where(IsUsable).Cast<Control>().ToList();
        var buttons = new[] { _confirmButton, _isMultiplayer ? _unreadyButton : null, _backButton }.Where(IsUsable).Cast<Control>().ToList();

        var firstChar = GetPreferredCharacterFocusTarget();
        var firstModifier = modifiers.FirstOrDefault();
        var firstButton = buttons.FirstOrDefault();

        if (seed != null)
        {
            var self = seed.GetPath();
            Control downTarget = firstChar ?? firstModifier ?? firstButton ?? (Control?)ascension ?? seed;
            seed.FocusNeighborTop = self;
            seed.FocusNeighborLeft = self;
            seed.FocusNeighborRight = self;
            seed.FocusNeighborBottom = downTarget.GetPath();
        }

        for (int i = 0; i < chars.Count; i++)
        {
            var self = chars[i].GetPath();
            chars[i].FocusNeighborLeft = i > 0 ? chars[i - 1].GetPath() : self;
            chars[i].FocusNeighborRight = i < chars.Count - 1 ? chars[i + 1].GetPath() : self;
            chars[i].FocusNeighborTop = seed?.GetPath() ?? self;
            chars[i].FocusNeighborBottom = ((Control?)ascension ?? firstModifier ?? firstButton ?? chars[i]).GetPath();
        }

        if (ascension != null)
        {
            Control targetAboveControl = firstChar as Control ?? seed as Control ?? ascension;
            var targetAbove = targetAboveControl.GetPath();
            var targetBelow = (firstModifier ?? firstButton ?? ascension).GetPath();
            var self = ascension.GetPath();
            ascension.FocusNeighborTop = targetAbove;
            ascension.FocusNeighborBottom = targetBelow;
            ascension.FocusNeighborLeft = self;
            ascension.FocusNeighborRight = self;

            if (IsUsable(_ascensionLeftArrow))
            {
                _ascensionLeftArrow!.FocusNeighborTop = targetAbove;
                _ascensionLeftArrow.FocusNeighborBottom = targetBelow;
                _ascensionLeftArrow.FocusNeighborLeft = _ascensionLeftArrow.GetPath();
                _ascensionLeftArrow.FocusNeighborRight = IsUsable(_ascensionRightArrow)
                    ? _ascensionRightArrow!.GetPath()
                    : self;
            }

            if (IsUsable(_ascensionRightArrow))
            {
                _ascensionRightArrow!.FocusNeighborTop = targetAbove;
                _ascensionRightArrow.FocusNeighborBottom = targetBelow;
                _ascensionRightArrow.FocusNeighborLeft = IsUsable(_ascensionLeftArrow)
                    ? _ascensionLeftArrow!.GetPath()
                    : self;
                _ascensionRightArrow.FocusNeighborRight = _ascensionRightArrow.GetPath();
            }
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            var self = modifiers[i].GetPath();
            modifiers[i].FocusNeighborLeft = i > 0 ? modifiers[i - 1].GetPath() : self;
            modifiers[i].FocusNeighborRight = i < modifiers.Count - 1 ? modifiers[i + 1].GetPath() : self;
            modifiers[i].FocusNeighborTop = (ascension ?? firstChar ?? seed ?? modifiers[i]).GetPath();
            modifiers[i].FocusNeighborBottom = (firstButton ?? modifiers[i]).GetPath();
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            var self = buttons[i].GetPath();
            buttons[i].FocusNeighborLeft = self;
            buttons[i].FocusNeighborRight = self;
            buttons[i].FocusNeighborTop = i > 0
                ? buttons[i - 1].GetPath()
                : (firstModifier ?? ascension ?? firstChar ?? seed ?? buttons[i]).GetPath();
            buttons[i].FocusNeighborBottom = i < buttons.Count - 1
                ? buttons[i + 1].GetPath()
                : self;
        }
    }

    private void EnsureFocus()
    {
        var focusOwner = _screen.GetViewport()?.GuiGetFocusOwner() as Control;
        if (focusOwner != null && GetElement(focusOwner) != null)
            return;

        GetInitialFocusTarget()?.GrabFocus();
    }

    private Control? GetInitialFocusTarget()
    {
        if (IsUsable(_seedInput))
            return _seedInput;
        if (_characterButtons.FirstOrDefault(IsUsable) is { } character)
            return character;
        if (IsUsable(_ascensionPanel))
            return _ascensionPanel;
        if (_modifierTickboxes.FirstOrDefault(IsUsable) is { } modifier)
            return modifier;
        if (IsUsable(_confirmButton))
            return _confirmButton;
        if (IsUsable(_unreadyButton))
            return _unreadyButton;
        if (IsUsable(_backButton))
            return _backButton;
        return null;
    }

    private NCharacterSelectButton? GetPreferredCharacterFocusTarget()
    {
        if (_lastFocusedCharacterButton != null && IsUsable(_lastFocusedCharacterButton))
            return _lastFocusedCharacterButton;

        var selected = _characterButtons.FirstOrDefault(b => IsUsable(b) && b.IsSelected);
        if (selected != null)
            return selected;

        return _characterButtons.FirstOrDefault(IsUsable);
    }

    private string BuildStateToken()
    {
        var parts = new List<string>
        {
            $"{_seedInput?.Text}|{_seedInput?.Editable}|{_seedInput?.Visible}",
            $"{_ascensionPanel?.Visible}|{_ascensionPanel?.Ascension}"
        };

        parts.AddRange(_characterButtons.Select(b => $"{b.GetInstanceId()}:{b.Visible}:{b.IsEnabled}:{b.IsLocked}"));
        parts.AddRange(_modifierTickboxes.Select(t => $"{t.GetInstanceId()}:{t.Visible}:{t.IsEnabled}:{t.IsTicked}"));
        parts.Add($"{_confirmButton?.Visible}:{_confirmButton?.IsEnabled}");
        parts.Add($"{_unreadyButton?.Visible}:{_unreadyButton?.IsEnabled}");
        parts.Add($"{_backButton?.Visible}:{_backButton?.IsEnabled}");
        return string.Join("|", parts);
    }

    private string? GetAscensionStatus()
    {
        if (_ascensionPanel == null)
            return null;

        var value = _ascensionPanel.Ascension;
        var title = AscensionHelper.GetTitle(value).GetFormattedText();
        var description = AscensionHelper.GetDescription(value).GetFormattedText();
        return string.IsNullOrWhiteSpace(description) ? title : $"{title}. {description}";
    }

    private UIElement GetOrCreate(Control control, Func<UIElement> factory)
    {
        if (_elementCache.TryGetValue(control.GetInstanceId(), out var existing))
            return existing;

        var created = factory();
        _elementCache[control.GetInstanceId()] = created;
        return created;
    }

    private static bool IsUsable(Control? control)
    {
        return control != null
            && GodotObject.IsInstanceValid(control)
            && control.Visible;
    }

    private string Ui(string key, object? data = null)
    {
        return data == null
            ? LocalizationManager.GetOrDefault("ui", key, key)
            : Message.Localized("ui", key, data).Resolve();
    }

    private static void Activate(NClickableControl? control)
    {
        if (control == null || !GodotObject.IsInstanceValid(control))
            return;

        control.EmitSignal(NClickableControl.SignalName.Released, control);
    }
}
