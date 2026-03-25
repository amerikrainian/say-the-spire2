using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using SayTheSpire2.Input;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CompendiumMenuScreen : GameScreen
{
    private readonly NCompendiumSubmenu _screen;
    private readonly NavigableContainer _root = new()
    {
        ContainerLabel = "Compendium",
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private NClickableControl? _backButton;

    public override string? ScreenName => "Compendium";

    public CompendiumMenuScreen(NCompendiumSubmenu screen)
    {
        _screen = screen;
        RootElement = _root;
        ClaimAction("ui_left");
        ClaimAction("ui_right");
        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
    }

    public override void OnPush()
    {
        base.OnPush();
        _root.FocusFirst();
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        return action.Key switch
        {
            "ui_right" or "ui_down" => _root.MoveRelative(1),
            "ui_left" or "ui_up" => _root.MoveRelative(-1),
            _ => _root.HandleAction(action),
        };
    }

    protected override void BuildRegistry()
    {
        _root.Clear();
        _backButton = null;

        RegisterButton("%CardLibraryButton");
        RegisterButton("%RelicCollectionButton");
        RegisterButton("%PotionLabButton");
        RegisterButton("%StatisticsButton");
        RegisterButton("%RunHistoryButton");
        RegisterButton("%ConfirmButton", isBack: true);
    }

    private void RegisterButton(string nodePath, bool isBack = false)
    {
        var control = _screen.GetNodeOrNull<NClickableControl>(nodePath);
        if (control == null || !control.Visible)
            return;

        var proxy = ProxyFactory.Create(control);
        var element = new ActionElement(
            () => proxy.GetLabel(),
            status: () => proxy.GetStatusString(),
            tooltip: () => proxy.GetTooltip(),
            typeKey: () => proxy.GetTypeKey(),
            extras: () => proxy.GetExtrasString(),
            isVisible: () => proxy.IsVisible,
            onActivated: () => control.EmitSignal(NClickableControl.SignalName.Released, control));
        _root.Add(element);
        Register(control, element);

        if (isBack)
            _backButton = control;
    }
}
