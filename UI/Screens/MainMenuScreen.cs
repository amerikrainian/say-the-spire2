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
    public override string? ScreenName => LocalizationManager.GetOrDefault("ui", "SCREENS.MAIN_MENU", "Main Menu");

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
        Log.Info($"[AccessibilityMod] MainMenuScreen built: {root.Children.Count} buttons");
    }
}
