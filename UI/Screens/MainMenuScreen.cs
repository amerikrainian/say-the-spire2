using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;
using ListContainer = SayTheSpire2.UI.Elements.ListContainer;

namespace SayTheSpire2.UI.Screens;

public class MainMenuScreen : GameScreen
{
    public override Message? ScreenName => Message.Localized("ui", "SCREENS.MAIN_MENU");

    private string? _stateToken;

    protected override void BuildRegistry()
    {
        var mainMenu = ActiveScreenContext.Instance.GetCurrentScreen() as NMainMenu;
        if (mainMenu == null)
        {
            Log.Error("[AccessibilityMod] MainMenuScreen: NMainMenu not found");
            return;
        }

        var root = new ListContainer { ContainerLabel = Message.Localized("ui", "CONTAINERS.MAIN_MENU"), AnnounceName = false };

        var buttonsContainer = mainMenu.GetNodeOrNull("MainMenuTextButtons");
        if (buttonsContainer == null)
        {
            Log.Error("[AccessibilityMod] MainMenuScreen: MainMenuTextButtons not found");
            return;
        }

        for (int i = 0; i < buttonsContainer.GetChildCount(); i++)
        {
            var child = buttonsContainer.GetChild(i);
            if (child is not NButton button) continue;
            if (!button.IsVisible() || !button.IsEnabled) continue;

            var proxy = new ProxyButton(button);
            root.Add(proxy);
            Register(button, proxy);
        }

        RootElement = root;
        _stateToken = BuildStateToken();
        Log.Info($"[AccessibilityMod] MainMenuScreen built: {root.Children.Count} buttons");
    }

    public override void OnUpdate()
    {
        var token = BuildStateToken();
        if (token == _stateToken) return;

        ClearRegistry();
        BuildRegistry();
    }

    /// <summary>
    /// Snapshot of the visible/enabled button set. The main menu mutates its
    /// button list at runtime — e.g. "Resume" appears or disappears depending
    /// on whether a save exists — so a one-shot registry build goes stale and
    /// position info ("3 of 6") starts lying. Rebuild whenever this token
    /// changes.
    /// </summary>
    private string? BuildStateToken()
    {
        var mainMenu = ActiveScreenContext.Instance.GetCurrentScreen() as NMainMenu;
        if (mainMenu == null) return null;

        var buttonsContainer = mainMenu.GetNodeOrNull("MainMenuTextButtons");
        if (buttonsContainer == null) return null;

        var sb = new StringBuilder();
        for (int i = 0; i < buttonsContainer.GetChildCount(); i++)
        {
            var child = buttonsContainer.GetChild(i);
            if (child is not NButton button) continue;
            sb.Append(button.Name).Append(':').Append(button.IsVisible()).Append(':').Append(button.IsEnabled).Append('|');
        }
        return sb.ToString();
    }
}
