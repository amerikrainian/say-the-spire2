using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Saves.Runs;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class DailyRunLoadGameScreen : Screen
{
    private static readonly FieldInfo? LobbyField =
        AccessTools.Field(typeof(NDailyRunLoadScreen), "_lobby");

    public static DailyRunLoadGameScreen? Current { get; private set; }

    private readonly NDailyRunLoadScreen _screen;
    private readonly NavigableContainer _nav = new()
    {
        ContainerLabel = Ui("DAILY_RUN.SCREEN_NAME"),
        AnnounceName = true,
        AnnouncePosition = true,
    };

    private readonly ActionElement _characterElement;
    private readonly ActionElement _dateElement;
    private readonly List<ActionElement> _modifierElements = new();
    private readonly ActionElement _leaderboardElement;
    private readonly ActionElement _scoreWarningElement;
    private readonly ActionElement _embarkElement;
    private readonly ActionElement _unreadyElement;
    private readonly ActionElement _backElement;

    private Label? _dateLabel;
    private List<NDailyRunScreenModifier> _modifierControls = new();
    private NDailyRunLeaderboard? _leaderboard;
    private Control? _scoreWarning;
    private NClickableControl? _embarkButton;
    private NClickableControl? _unreadyButton;
    private NClickableControl? _backButton;
    public override string? ScreenName => Ui("DAILY_RUN.SCREEN_NAME");

    public DailyRunLoadGameScreen(NDailyRunLoadScreen screen)
    {
        _screen = screen;
        RootElement = _nav;

        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
        ClaimAction("ui_cancel");
        ClaimAction("mega_pause_and_back");

        _characterElement = new ActionElement(
            () => GetCharacterLabel(),
            status: () => GetCharacterStatus());
        _dateElement = new ActionElement(
            () => GetDateText() ?? Ui("DAILY_RUN.SAVE_FALLBACK"));
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            _modifierElements.Add(new ActionElement(
                () => GetModifierText(index) ?? Ui("DAILY_RUN.MODIFIER", new { index = index + 1 }),
                isVisible: () => GetModifierVisible(index)));
        }
        _leaderboardElement = new ActionElement(
            () => Ui("DAILY_RUN.LEADERBOARD"),
            typeKey: () => "button",
            onActivated: OpenLeaderboard);
        _scoreWarningElement = new ActionElement(
            () => new LocString("main_menu_ui", "DAILY_RUN_MENU.NO_UPLOAD_HOVERTIP.title").GetFormattedText(),
            tooltip: () => new LocString("main_menu_ui", "DAILY_RUN_MENU.NO_UPLOAD_HOVERTIP.description").GetFormattedText(),
            isVisible: () => _scoreWarning?.Visible == true);
        _embarkElement = new ActionElement(
            () => GetEmbarkLabel(),
            status: () => GetButtonStatus(_embarkButton),
            typeKey: () => "button",
            isVisible: () => IsVisible(_embarkButton),
            onActivated: () => Activate(_embarkButton));
        _unreadyElement = new ActionElement(
            () => Ui("DAILY_RUN.CANCEL_READY"),
            status: () => GetButtonStatus(_unreadyButton),
            typeKey: () => "button",
            isVisible: () => IsVisible(_unreadyButton),
            onActivated: () => Activate(_unreadyButton));
        _backElement = new ActionElement(
            () => Ui("DAILY_RUN.BACK"),
            status: () => GetButtonStatus(_backButton),
            typeKey: () => "button",
            isVisible: () => IsVisible(_backButton),
            onActivated: () => Activate(_backButton));

        _nav.Add(_characterElement);
        _nav.Add(_dateElement);
        foreach (var modifier in _modifierElements)
            _nav.Add(modifier);
        _nav.Add(_leaderboardElement);
        _nav.Add(_scoreWarningElement);
        _nav.Add(_embarkElement);
        _nav.Add(_unreadyElement);
        _nav.Add(_backElement);
    }

    public override void OnPush()
    {
        Current = this;
        ResolveControls();
        _nav.FocusFirst();
    }

    public override void OnPop()
    {
        if (Current == this) Current = null;
    }

    public override void OnFocus()
    {
        if (_nav.FocusedChild == null || !_nav.FocusedChild.IsVisible)
            _nav.FocusFirst();
    }

    public override void OnUpdate()
    {
        ResolveControls();
        if (_nav.FocusedChild == null || !_nav.FocusedChild.IsVisible)
            _nav.FocusFirst();
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            case "ui_cancel":
            case "mega_pause_and_back":
                Activate(_backButton);
                return true;
            default:
                return _nav.HandleAction(action);
        }
    }

    public void OnPlayerConnected(ulong playerId)
    {
        if (!IsMultiplayer())
            return;

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_JOINED", new { player = GetPlayerName(playerId) }));
    }

    public void OnPlayerReadyChanged(ulong playerId)
    {
        if (!IsMultiplayer())
            return;

        var lobby = Lobby;
        if (lobby != null && playerId == lobby.NetService.NetId)
            return;

        var status = Lobby?.IsPlayerReady(playerId) == true ? Ui("DAILY_RUN.READY") : Ui("DAILY_RUN.NOT_READY");
        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOAD_LOBBY_CHANGED", new
        {
            player = GetPlayerName(playerId),
            status,
        }));
    }

    public void OnPlayerDisconnected(ulong playerId)
    {
        if (!IsMultiplayer())
            return;

        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_LEFT", new { player = GetPlayerName(playerId) }));
    }

    public void OnLocalDisconnected()
    {
        if (IsMultiplayer())
            SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.LOBBY_DISCONNECTED"));
    }

    public void OnEmbarkPressed()
    {
        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.MARKED_READY"));
    }

    public void OnUnreadyPressed()
    {
        SpeechManager.Output(Message.Localized("ui", "DAILY_RUN.NO_LONGER_READY"));
    }

    private void ResolveControls()
    {
        if (!GodotObject.IsInstanceValid(_screen))
            return;

        _dateLabel ??= _screen.GetNodeOrNull<Label>("%Date");
        if (_modifierControls.Count == 0)
        {
            var container = _screen.GetNodeOrNull<Control>("%ModifiersContainer");
            if (container != null)
                _modifierControls = container.GetChildren().OfType<NDailyRunScreenModifier>().ToList();
        }
        _leaderboard ??= _screen.GetNodeOrNull<NDailyRunLeaderboard>("%Leaderboards");
        _scoreWarning ??= _leaderboard?.GetNodeOrNull<Control>("%ScoreWarning");
        _embarkButton ??= _screen.GetNodeOrNull<NClickableControl>("%ConfirmButton");
        _unreadyButton ??= _screen.GetNodeOrNull<NClickableControl>("%UnreadyButton");
        _backButton ??= _screen.GetNodeOrNull<NClickableControl>("%BackButton");
    }

    private LoadRunLobby? Lobby => LobbyField?.GetValue(_screen) as LoadRunLobby;

    private bool IsMultiplayer()
    {
        return Lobby?.NetService.Type is NetGameType.Host or NetGameType.Client;
    }

    private string GetCharacterLabel()
    {
        var player = GetLocalSerializablePlayer();
        return player?.CharacterId.ToString() ?? Ui("DAILY_RUN.CHARACTER");
    }

    private string? GetCharacterStatus()
    {
        var lobby = Lobby;
        var player = GetLocalSerializablePlayer();
        if (lobby == null || player == null)
            return null;

        var parts = new List<string>();
        if (IsMultiplayer())
            parts.Add(GetPlayerName(player.NetId));
        parts.Add(Ui("DAILY_RUN.ASCENSION", new { value = lobby.Run.Ascension }));
        if (lobby.IsPlayerReady(player.NetId))
            parts.Add(Ui("DAILY_RUN.READY"));
        return string.Join(", ", parts);
    }

    private SerializablePlayer? GetLocalSerializablePlayer()
    {
        var lobby = Lobby;
        return lobby?.Run.Players.FirstOrDefault(p => p.NetId == lobby.NetService.NetId);
    }

    private string? GetDateText()
    {
        return _dateLabel?.Text;
    }

    private string? GetModifierText(int index)
    {
        if (index < 0 || index >= _modifierControls.Count)
            return null;
        var description = _modifierControls[index].GetNodeOrNull<RichTextLabel>("Description");
        return description == null ? null : ProxyElement.StripBbcode(description.Text);
    }

    private bool GetModifierVisible(int index)
    {
        return index >= 0 && index < _modifierControls.Count && _modifierControls[index].Visible;
    }

    private void OpenLeaderboard()
    {
        if (_leaderboard != null)
            ScreenManager.PushScreen(new DailyLeaderboardScreen(_leaderboard));
    }

    private string GetEmbarkLabel()
    {
        return Ui("DAILY_RUN.READY");
    }

    private string GetPlayerName(ulong playerId)
    {
        return MultiplayerHelper.GetPlayerName(playerId, Lobby?.NetService.Platform);
    }

    private static bool IsVisible(Control? control)
    {
        return control != null && control.Visible;
    }

    private static string? GetButtonStatus(NClickableControl? control)
    {
        if (control == null || control.IsEnabled)
            return null;

        return Ui("DAILY_RUN.DISABLED");
    }

    private static void Activate(NClickableControl? control)
    {
        if (control == null || !GodotObject.IsInstanceValid(control))
            return;

        control.EmitSignal(NClickableControl.SignalName.Released, control);
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }

    private static string Ui(string key, object vars)
    {
        return Message.Localized("ui", key, vars).Resolve();
    }
}
