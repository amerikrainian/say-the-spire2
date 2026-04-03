using System.Linq;
using Godot;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Screens;

public class ListenScreen : Screen
{
    private readonly BindingSetting _setting;
    private readonly bool _isController;
    private readonly InputBinding? _replacing;
    private readonly PanelContainer _root;
    private bool _listening;

    public override string? ScreenName => LocalizationManager.GetOrDefault("ui", "SCREENS.LISTEN", "Listen");

    public ListenScreen(BindingSetting setting, bool isController, InputBinding? replacing = null)
    {
        _setting = setting;
        _isController = isController;
        _replacing = replacing;

        _root = new PanelContainer { Name = "ListenScreen" };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var bg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f) };
        _root.AddThemeStyleboxOverride("panel", bg);

        var centerContainer = new CenterContainer();
        _root.AddChild(centerContainer);

        var label = new Label
        {
            Text = isController ? LocalizationManager.GetOrDefault("ui", "LISTEN.PRESS_BUTTON", "Press a button...") : LocalizationManager.GetOrDefault("ui", "LISTEN.PRESS_KEY", "Press a key..."),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        label.AddThemeFontSizeOverride("font_size", 24);
        centerContainer.AddChild(label);

        // Claim all input so nothing leaks through
        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_left");
        ClaimAction("ui_right");
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
        ClaimAction("ui_cancel");
        ClaimAction("mega_pause_and_back");
        ClaimAction("mega_peek");
        ClaimAction("mega_view_draw_pile");
        ClaimAction("mega_view_discard_pile");
        ClaimAction("mega_view_deck_and_tab_left");
        ClaimAction("mega_view_exhaust_pile_and_tab_right");
        ClaimAction("mega_view_map");
        ClaimAction("mega_top_panel");
        ClaimAction("mod_settings");
        ClaimAction("buffer_next_item");
        ClaimAction("buffer_prev_item");
        ClaimAction("buffer_next");
        ClaimAction("buffer_prev");
        ClaimAction("announce_gold");
        ClaimAction("announce_hp");
        ClaimAction("announce_block");
        ClaimAction("announce_energy");
        ClaimAction("announce_powers");
        ClaimAction("announce_intents");
    }

    public override void OnPush()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(_root);
        _listening = true;

        var prompt = _isController ? LocalizationManager.GetOrDefault("ui", "LISTEN.PRESS_BUTTON", "Press a button...") : LocalizationManager.GetOrDefault("ui", "LISTEN.PRESS_KEY", "Press a key...");
        var cancelHint = GetCancelHint();
        if (cancelHint != null)
            prompt += $" {cancelHint} to cancel.";
        SpeechManager.Output(Message.Raw(prompt));

        InputManager.StartListening(OnInputCaptured);
    }

    public override void OnFocus()
    {
        if (GodotObject.IsInstanceValid(_root))
            _root.Visible = true;
    }

    public override void OnUnfocus()
    {
        if (GodotObject.IsInstanceValid(_root))
            _root.Visible = false;
    }

    public override void OnPop()
    {
        _listening = false;
        InputManager.StopListening();

        if (GodotObject.IsInstanceValid(_root))
        {
            _root.GetParent()?.RemoveChild(_root);
            _root.QueueFree();
        }
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        // Consume everything while listening
        return true;
    }

    private void OnInputCaptured(InputBinding binding)
    {
        if (!_listening) return;

        // Wrong type — ignore (e.g. keyboard press while listening for controller)
        if (_isController && binding is not ControllerBinding) return;
        if (!_isController && binding is not KeyboardBinding) return;

        // If pressed input matches an existing binding on this action, treat as cancel
        if (IsExistingBinding(binding))
        {
            ScreenManager.RemoveScreen(this);
            SpeechManager.Output(Message.Raw(LocalizationManager.GetOrDefault("ui", "SPEECH.CANCELLED", "Cancelled")));
            return;
        }

        // Check for conflicts with other actions
        var conflict = FindConflict(binding);
        if (conflict != null)
        {
            ScreenManager.RemoveScreen(this);
            SpeechManager.Output(Message.Localized("ui", "SPEECH.ALREADY_BOUND", new { key = binding.DisplayName, action = conflict.Label }));
            return;
        }

        var action = _setting.Action;

        if (_replacing != null)
            action.RemoveBinding(_replacing);

        action.AddBinding(binding);

        ScreenManager.RemoveScreen(this);
        SpeechManager.Output(Message.Localized("ui", "SPEECH.BOUND_TO", new { key = binding.DisplayName }));
    }

    private InputAction? FindConflict(InputBinding captured)
    {
        foreach (var action in InputManager.Actions)
        {
            if (action == _setting.Action)
                continue;

            foreach (var existing in action.Bindings)
            {
                if (BindingsMatch(captured, existing))
                    return action;
            }
        }
        return null;
    }

    private static bool BindingsMatch(InputBinding a, InputBinding b)
    {
        if (a is KeyboardBinding ka && b is KeyboardBinding kb)
            return ka.Keycode == kb.Keycode && ka.Ctrl == kb.Ctrl
                && ka.Shift == kb.Shift && ka.Alt == kb.Alt;
        if (a is ControllerBinding ca && b is ControllerBinding cb)
            return ca.Input == cb.Input && ca.Modifier == cb.Modifier;
        return false;
    }

    private bool IsExistingBinding(InputBinding captured)
    {
        foreach (var existing in _setting.Action.Bindings)
        {
            if (existing == _replacing)
                continue;

            if (captured is KeyboardBinding kb && existing is KeyboardBinding ekb)
            {
                if (kb.Keycode == ekb.Keycode && kb.Ctrl == ekb.Ctrl
                    && kb.Shift == ekb.Shift && kb.Alt == ekb.Alt)
                    return true;
            }
            else if (captured is ControllerBinding cb && existing is ControllerBinding ecb)
            {
                if (cb.Input == ecb.Input && cb.Modifier == ecb.Modifier)
                    return true;
            }
        }
        return false;
    }

    private string? GetCancelHint()
    {
        var matchType = _isController ? typeof(ControllerBinding) : typeof(KeyboardBinding);
        var existing = _setting.Action.Bindings
            .Where(b => b.GetType() == matchType && b != _replacing)
            .FirstOrDefault();
        return existing?.DisplayName;
    }
}
