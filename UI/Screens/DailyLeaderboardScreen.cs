using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class DailyLeaderboardScreen : Screen
{
    private readonly DailyLeaderboardAdapter _adapter;
    private readonly NavigableContainer _nav = new()
    {
        ContainerLabel = Ui("DAILY_RUN.LEADERBOARD"),
        AnnounceName = true,
        AnnouncePosition = true,
    };

    private string? _lastStateToken;

    public override string? ScreenName => Ui("DAILY_RUN.LEADERBOARD");

    public DailyLeaderboardScreen(NDailyRunLeaderboard leaderboard)
    {
        _adapter = new DailyLeaderboardAdapter(leaderboard);
        RootElement = _nav;

        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_left");
        ClaimAction("ui_right");
        ClaimAction("daily_leaderboard_prev_page");
        ClaimAction("daily_leaderboard_next_page");
        ClaimAction("ui_cancel");
        ClaimAction("mega_pause_and_back");
    }

    public override void OnPush()
    {
        Rebuild(forceFocusFirst: true);
    }

    public override void OnFocus()
    {
        if (_nav.FocusedChild == null || !_nav.FocusedChild.IsVisible)
            _nav.FocusFirst();
    }

    public override void OnUpdate()
    {
        var token = _adapter.GetStateToken();
        if (token != _lastStateToken)
            Rebuild(forceFocusFirst: false);

        if (_nav.FocusedChild == null || !_nav.FocusedChild.IsVisible)
            _nav.FocusFirst();
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            case "ui_cancel":
            case "mega_pause_and_back":
                ScreenManager.RemoveScreen(this);
                return true;
            case "ui_left":
                TryChangeDay(-1);
                return true;
            case "ui_right":
                TryChangeDay(1);
                return true;
            case "daily_leaderboard_prev_page":
                _adapter.ChangePage(-1);
                return true;
            case "daily_leaderboard_next_page":
                _adapter.ChangePage(1);
                return true;
            default:
                return _nav.HandleAction(action);
        }
    }

    private void Rebuild(bool forceFocusFirst)
    {
        var previousIndex = _nav.FocusIndex;
        _nav.Clear();

        foreach (var entry in _adapter.GetEntries())
        {
            _nav.Add(new ActionElement(
                () => entry.Label,
                status: () => entry.Status));
        }

        if (_adapter.IsLoading)
        {
            _nav.Add(new ActionElement(() => Ui("DAILY_RUN_LEADERBOARD.LOADING_SCORES")));
        }
        else if (_adapter.HasNoScores)
        {
            _nav.Add(new ActionElement(() => Ui("DAILY_RUN_LEADERBOARD.NO_SCORES")));
        }
        else if (_adapter.HasNoFriends)
        {
            _nav.Add(new ActionElement(() => Ui("DAILY_RUN_LEADERBOARD.NO_FRIENDS")));
        }

        if (_adapter.HasScoreWarning)
        {
            _nav.Add(new ActionElement(
                () => Ui("DAILY_RUN_LEADERBOARD.SCORE_WARNING"),
                tooltip: () => Ui("DAILY_RUN_LEADERBOARD.SCORE_WARNING_TOOLTIP")));
        }

        _lastStateToken = _adapter.GetStateToken();
        if (forceFocusFirst || previousIndex < 0)
            _nav.FocusFirst();
        else
            _nav.SetFocusIndex(previousIndex);
    }

    private void TryChangeDay(int delta)
    {
        if (!_adapter.SupportsDayNavigation())
            return;

        if (delta < 0)
        {
            if (_adapter.CanChangeDayPrevious())
                _adapter.ChangeDay(-1);
            return;
        }

        if (_adapter.CanChangeDayNext())
            _adapter.ChangeDay(1);
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }
}
