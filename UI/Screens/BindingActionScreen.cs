using Godot;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class BindingActionScreen : Screen
{
    private readonly BindingSetting _setting;
    private readonly InputBinding _binding;
    private readonly PanelContainer _root;
    private readonly NavigableContainer _navContainer;

    public override string? ScreenName => _binding.DisplayName;

    public BindingActionScreen(BindingSetting setting, InputBinding binding)
    {
        _setting = setting;
        _binding = binding;

        _root = new PanelContainer { Name = "BindingAction" };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var bg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f) };
        _root.AddThemeStyleboxOverride("panel", bg);

        var centerContainer = new CenterContainer();
        _root.AddChild(centerContainer);

        var contentPanel = new PanelContainer();
        var contentBg = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f, 1f),
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 32,
            ContentMarginRight = 32,
            ContentMarginTop = 24,
            ContentMarginBottom = 24,
        };
        contentPanel.AddThemeStyleboxOverride("panel", contentBg);
        contentPanel.CustomMinimumSize = new Vector2(500, 0);
        centerContainer.AddChild(contentPanel);

        var outerVBox = new VBoxContainer();
        outerVBox.AddThemeConstantOverride("separation", 16);
        contentPanel.AddChild(outerVBox);

        var title = new Label
        {
            Text = binding.DisplayName,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        outerVBox.AddChild(title);
        outerVBox.AddChild(new HSeparator());

        var itemList = new VBoxContainer();
        itemList.AddThemeConstantOverride("separation", 8);
        outerVBox.AddChild(itemList);

        _navContainer = new NavigableContainer
        {
            ContainerLabel = binding.DisplayName,
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = _navContainer;

        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
        ClaimAction("ui_cancel");
        ClaimAction("mega_pause_and_back");

        // Replace button
        bool isController = binding is ControllerBinding;
        var replaceBtn = new ButtonElement(LocalizationManager.GetOrDefault("ui", "BUTTONS.REPLACE", "Replace"));
        replaceBtn.OnActivated = () =>
        {
            var screen = new ListenScreen(_setting, isController, _binding);
            ScreenManager.PushScreen(screen);
        };
        _navContainer.Add(replaceBtn);
        AddButton(itemList, replaceBtn);

        // Delete button
        var deleteBtn = new ButtonElement(LocalizationManager.GetOrDefault("ui", "BUTTONS.DELETE", "Delete"));
        deleteBtn.OnActivated = () =>
        {
            _setting.Action.RemoveBinding(_binding);
            ScreenManager.RemoveScreen(this);
            SpeechManager.Output(Message.Localized("ui", "SPEECH.DELETED"));
        };
        _navContainer.Add(deleteBtn);
        AddButton(itemList, deleteBtn);
    }

    public override void OnPush()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(_root);
        _navContainer.FocusFirst();
    }

    public override void OnFocus()
    {
        if (GodotObject.IsInstanceValid(_root))
            _root.Visible = true;
        _navContainer.FocusFirst();
    }

    public override void OnUnfocus()
    {
        if (GodotObject.IsInstanceValid(_root))
            _root.Visible = false;
    }

    public override void OnPop()
    {
        if (GodotObject.IsInstanceValid(_root))
        {
            _root.GetParent()?.RemoveChild(_root);
            _root.QueueFree();
        }
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (action.Key == "ui_cancel")
        {
            ScreenManager.RemoveScreen(this);
            SpeechManager.Output(Message.Localized("ui", "SPEECH.CANCELLED"));
            return true;
        }

        return _navContainer.HandleAction(action);
    }

    private void AddButton(VBoxContainer list, ButtonElement button)
    {
        var control = (Control)button.Node;
        control.FocusMode = Control.FocusModeEnum.All;
        list.AddChild(control);
        control.FocusEntered += () => _navContainer.SetFocusTo(button);
        ((BaseButton)control).Pressed += () => button.Activate();
    }
}
