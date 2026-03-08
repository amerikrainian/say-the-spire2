using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Input;

public static class InputManager
{
    private static readonly List<InputAction> _actions = new();
    private static readonly HashSet<InputAction> _activeActions = new();

    private static readonly HashSet<Key> _modifierKeys = new()
    {
        Key.Ctrl, Key.Shift, Key.Alt,
    };

    private static readonly HashSet<string> _navActions = new()
    {
        "ui_up", "ui_down", "ui_left", "ui_right",
        "ui_accept", "ui_cancel", "ui_select"
    };

    /// <summary>
    /// When true, the mod intercepts all keyboard input and suppresses the game's
    /// default handling. Set to false to let the game handle input normally.
    /// </summary>
    public static bool InterceptInput { get; set; } = true;

    private static NControllerManager? _controllerManager;

    private static readonly FieldInfo? ListeningEntryField =
        typeof(NInputSettingsPanel).GetField("_listeningEntry", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly PropertyInfo IsUsingControllerProp =
        typeof(NControllerManager).GetProperty("IsUsingController", BindingFlags.Instance | BindingFlags.Public)!;

    private static readonly FieldInfo? LastMouseField =
        AccessTools.Field(typeof(NControllerManager), "_lastMousePosition");

    public static void Initialize()
    {
        RegisterGameActions();
        RegisterModActions();
        Log.Info($"[AccessibilityMod] InputManager initialized with {_actions.Count} actions.");
    }

    private static void RegisterGameActions()
    {
        _actions.Add(new InputAction("ui_accept", gameAction: "ui_accept")
            .AddBinding(Key.E)
            .AddBinding(ControllerInput.Y));
        _actions.Add(new InputAction("ui_select", gameAction: "ui_select")
            .AddBinding(Key.Enter)
            .AddBinding(ControllerInput.A));
        _actions.Add(new InputAction("ui_cancel", gameAction: "ui_cancel")
            .AddBinding(Key.Escape)
            .AddBinding(ControllerInput.B));
        _actions.Add(new InputAction("ui_up", gameAction: "ui_up")
            .AddBinding(Key.Up)
            .AddBinding(ControllerInput.DpadUp)
            .AddBinding(ControllerInput.LeftStickUp));
        _actions.Add(new InputAction("ui_down", gameAction: "ui_down")
            .AddBinding(Key.Down)
            .AddBinding(ControllerInput.DpadDown)
            .AddBinding(ControllerInput.LeftStickDown));
        _actions.Add(new InputAction("ui_left", gameAction: "ui_left")
            .AddBinding(Key.Left)
            .AddBinding(ControllerInput.DpadLeft)
            .AddBinding(ControllerInput.LeftStickLeft));
        _actions.Add(new InputAction("ui_right", gameAction: "ui_right")
            .AddBinding(Key.Right)
            .AddBinding(ControllerInput.DpadRight)
            .AddBinding(ControllerInput.LeftStickRight));
        _actions.Add(new InputAction("mega_peek", gameAction: "mega_peek")
            .AddBinding(Key.Space)
            .AddBinding(ControllerInput.LeftStickClick));
        _actions.Add(new InputAction("mega_view_draw_pile", gameAction: "mega_view_draw_pile")
            .AddBinding(Key.A)
            .AddBinding(ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("mega_view_discard_pile", gameAction: "mega_view_discard_pile")
            .AddBinding(Key.S)
            .AddBinding(ControllerInput.RightTrigger));
        _actions.Add(new InputAction("mega_view_deck_and_tab_left", gameAction: "mega_view_deck_and_tab_left")
            .AddBinding(Key.D)
            .AddBinding(ControllerInput.LeftShoulder));
        _actions.Add(new InputAction("mega_view_exhaust_pile_and_tab_right", gameAction: "mega_view_exhaust_pile_and_tab_right")
            .AddBinding(Key.X)
            .AddBinding(ControllerInput.RightShoulder));
        _actions.Add(new InputAction("mega_view_map", gameAction: "mega_view_map")
            .AddBinding(Key.M)
            .AddBinding(ControllerInput.Back));
        _actions.Add(new InputAction("mega_pause_and_back", gameAction: "mega_pause_and_back")
            .AddBinding(Key.Escape)
            .AddBinding(ControllerInput.Start));
        _actions.Add(new InputAction("mega_top_panel", gameAction: "mega_top_panel")
            .AddBinding(Key.Tab)
            .AddBinding(ControllerInput.X));
        _actions.Add(new InputAction("mega_select_card_1", gameAction: "mega_select_card_1").AddBinding(Key.Key1));
        _actions.Add(new InputAction("mega_select_card_2", gameAction: "mega_select_card_2").AddBinding(Key.Key2));
        _actions.Add(new InputAction("mega_select_card_3", gameAction: "mega_select_card_3").AddBinding(Key.Key3));
        _actions.Add(new InputAction("mega_select_card_4", gameAction: "mega_select_card_4").AddBinding(Key.Key4));
        _actions.Add(new InputAction("mega_select_card_5", gameAction: "mega_select_card_5").AddBinding(Key.Key5));
        _actions.Add(new InputAction("mega_select_card_6", gameAction: "mega_select_card_6").AddBinding(Key.Key6));
        _actions.Add(new InputAction("mega_select_card_7", gameAction: "mega_select_card_7").AddBinding(Key.Key7));
        _actions.Add(new InputAction("mega_select_card_8", gameAction: "mega_select_card_8").AddBinding(Key.Key8));
        _actions.Add(new InputAction("mega_select_card_9", gameAction: "mega_select_card_9").AddBinding(Key.Key9));
        _actions.Add(new InputAction("mega_select_card_10", gameAction: "mega_select_card_10").AddBinding(Key.Key0));
        _actions.Add(new InputAction("mega_release_card", gameAction: "mega_release_card").AddBinding(Key.Down));
    }

    private static void RegisterModActions()
    {
        _actions.Add(new InputAction("buffer_next_item").AddBinding(Key.Up, ctrl: true));
        _actions.Add(new InputAction("buffer_prev_item").AddBinding(Key.Down, ctrl: true));
        _actions.Add(new InputAction("buffer_next").AddBinding(Key.Right, ctrl: true));
        _actions.Add(new InputAction("buffer_prev").AddBinding(Key.Left, ctrl: true));
        _actions.Add(new InputAction("reset_bindings").AddBinding(Key.R, ctrl: true, shift: true));
        _actions.Add(new InputAction("announce_gold").AddBinding(Key.G, ctrl: true));
        _actions.Add(new InputAction("announce_hp").AddBinding(Key.H, ctrl: true));
        _actions.Add(new InputAction("announce_block").AddBinding(Key.B, ctrl: true));
        _actions.Add(new InputAction("announce_energy").AddBinding(Key.Y, ctrl: true));
        _actions.Add(new InputAction("announce_powers").AddBinding(Key.P, ctrl: true));
        _actions.Add(new InputAction("announce_intents").AddBinding(Key.I, ctrl: true));
        _actions.Add(new InputAction("mod_settings").AddBinding(Key.M, ctrl: true));
    }

    /// <summary>
    /// Maps game controller action names (from InputEventAction) to ControllerInput.
    /// These arrive pre-remapped by the game's input system.
    /// </summary>
    private static readonly Dictionary<string, ControllerInput> _controllerActionMap = new()
    {
        { "controller_d_pad_north", ControllerInput.DpadUp },
        { "controller_d_pad_south", ControllerInput.DpadDown },
        { "controller_d_pad_west", ControllerInput.DpadLeft },
        { "controller_d_pad_east", ControllerInput.DpadRight },
        { "controller_face_button_north", ControllerInput.Y },
        { "controller_face_button_south", ControllerInput.A },
        { "controller_face_button_east", ControllerInput.B },
        { "controller_face_button_west", ControllerInput.X },
        { "controller_left_bumper", ControllerInput.LeftShoulder },
        { "controller_right_bumper", ControllerInput.RightShoulder },
        { "controller_left_trigger", ControllerInput.LeftTrigger },
        { "controller_right_trigger", ControllerInput.RightTrigger },
        { "controller_select_button", ControllerInput.Back },
        { "controller_start_button", ControllerInput.Start },
        { "controller_joystick_press", ControllerInput.LeftStickClick },
        { "controller_joystick_up", ControllerInput.LeftStickUp },
        { "controller_joystick_down", ControllerInput.LeftStickDown },
        { "controller_joystick_left", ControllerInput.LeftStickLeft },
        { "controller_joystick_right", ControllerInput.LeftStickRight },
    };

    /// <summary>
    /// Maps raw JoyButton values to our ControllerInput enum.
    /// Used as fallback if raw joypad events arrive instead of actions.
    /// </summary>
    private static readonly Dictionary<JoyButton, ControllerInput> _joyButtonMap = new()
    {
        { JoyButton.DpadUp, ControllerInput.DpadUp },
        { JoyButton.DpadDown, ControllerInput.DpadDown },
        { JoyButton.DpadLeft, ControllerInput.DpadLeft },
        { JoyButton.DpadRight, ControllerInput.DpadRight },
        { JoyButton.A, ControllerInput.A },
        { JoyButton.B, ControllerInput.B },
        { JoyButton.X, ControllerInput.X },
        { JoyButton.Y, ControllerInput.Y },
        { JoyButton.LeftShoulder, ControllerInput.LeftShoulder },
        { JoyButton.RightShoulder, ControllerInput.RightShoulder },
        { JoyButton.LeftStick, ControllerInput.LeftStickClick },
        { JoyButton.RightStick, ControllerInput.RightStickClick },
        { JoyButton.Start, ControllerInput.Start },
        { JoyButton.Back, ControllerInput.Back },
    };

    /// <summary>
    /// Maps JoyAxis + direction to ControllerInput for stick and trigger inputs.
    /// Positive = right/down/trigger pressed, Negative = left/up.
    /// </summary>
    private static readonly Dictionary<(JoyAxis, bool positive), ControllerInput> _joyAxisMap = new()
    {
        { (JoyAxis.LeftX, false), ControllerInput.LeftStickLeft },
        { (JoyAxis.LeftX, true), ControllerInput.LeftStickRight },
        { (JoyAxis.LeftY, false), ControllerInput.LeftStickUp },
        { (JoyAxis.LeftY, true), ControllerInput.LeftStickDown },
        { (JoyAxis.RightX, false), ControllerInput.RightStickLeft },
        { (JoyAxis.RightX, true), ControllerInput.RightStickRight },
        { (JoyAxis.RightY, false), ControllerInput.RightStickUp },
        { (JoyAxis.RightY, true), ControllerInput.RightStickDown },
        { (JoyAxis.TriggerLeft, true), ControllerInput.LeftTrigger },
        { (JoyAxis.TriggerRight, true), ControllerInput.RightTrigger },
    };

    private const float StickDeadzone = 0.5f;

    /// <summary>
    /// Tracks which axis-based ControllerInputs are currently "pressed" so we can
    /// detect press/release transitions for analog inputs.
    /// </summary>
    private static readonly HashSet<ControllerInput> _activeAxisInputs = new();

    /// <summary>
    /// Called from _Input prefix on NControllerManager. Updates key states,
    /// matches actions immediately, and consumes the event.
    /// Returns true if the event was consumed.
    /// </summary>
    public static bool OnInputEvent(NControllerManager controller, InputEvent inputEvent)
    {
        if (!InterceptInput || IsGameListeningForRebind())
            return false;

        _controllerManager = controller;

        if (inputEvent is InputEventKey keyEvent)
        {
            if (keyEvent.Echo)
                return true; // consume but don't process

            if (keyEvent.Pressed)
                OnKeyPressed(keyEvent);
            else
                OnKeyReleased(keyEvent);

            return true;
        }

        if (inputEvent is InputEventJoypadButton or InputEventJoypadMotion)
        {
            // Always consume raw joypad events — process matched ones,
            // silently swallow unmatched to prevent leaking to the game.
            OnControllerEvent(inputEvent);
            return true;
        }

        // Controller inputs arrive as InputEventAction (already remapped by the game).
        // Handle controller_* actions via ControllerInput matching; ignore the
        // remapped ui_*/mega_* duplicates. Always consume to prevent leaking.
        if (inputEvent is InputEventAction actionEvent)
        {
            OnControllerActionEvent(actionEvent);
            return true;
        }

        return false;
    }

    private static void OnKeyPressed(InputEventKey keyEvent)
    {
        // Any keypress interrupts current speech
        Speech.SpeechManager.Silence();

        // If it's a modifier key, don't trigger actions — just wait for the
        // non-modifier key that completes the combo
        if (_modifierKeys.Contains(keyEvent.Keycode))
            return;

        // Find matching actions based on keycode + current modifiers
        bool anyConsumed = false;
        foreach (var action in _actions)
        {
            if (_activeActions.Contains(action))
                continue;

            if (action.MatchesKeyEvent(keyEvent))
            {
                _activeActions.Add(action);
                EnsureFocusMode(action);

                // If any action for this key was consumed by the screen stack,
                // don't inject game actions for remaining matches either
                if (!anyConsumed)
                {
                    bool consumed = ScreenManager.DispatchAction(action, InputActionState.JustPressed);
                    if (consumed)
                        anyConsumed = true;
                    else if (action.GameAction != null)
                        InjectGameAction(action.GameAction, pressed: true);
                }
            }
        }
    }

    private static void OnKeyReleased(InputEventKey keyEvent)
    {
        // Check which active actions are no longer satisfied
        // (released key was part of their binding)
        var toRelease = new List<InputAction>();
        foreach (var action in _activeActions)
        {
            if (action.UsesKey(keyEvent.Keycode))
                toRelease.Add(action);
        }

        foreach (var action in toRelease)
        {
            _activeActions.Remove(action);
            bool consumed = ScreenManager.DispatchAction(action, InputActionState.JustReleased);
            if (!consumed && action.GameAction != null)
                InjectGameAction(action.GameAction, pressed: false);
        }
    }

    private static bool OnControllerEvent(InputEvent inputEvent)
    {
        if (inputEvent is InputEventJoypadButton buttonEvent)
        {
            if (!_joyButtonMap.TryGetValue(buttonEvent.ButtonIndex, out var input))
                return false;

            if (buttonEvent.Pressed)
                return OnControllerInputPressed(input);
            else
                return OnControllerInputReleased(input);
        }

        if (inputEvent is InputEventJoypadMotion motionEvent)
        {
            return OnAxisMotion(motionEvent);
        }

        return false;
    }

    private static bool OnAxisMotion(InputEventJoypadMotion motionEvent)
    {
        bool anyHandled = false;
        var axis = motionEvent.Axis;
        var value = motionEvent.AxisValue;

        // Check both directions for this axis
        var positiveKey = (axis, true);
        var negativeKey = (axis, false);

        if (_joyAxisMap.TryGetValue(positiveKey, out var positiveInput))
        {
            bool nowPressed = value > StickDeadzone;
            bool wasPressed = _activeAxisInputs.Contains(positiveInput);

            if (nowPressed && !wasPressed)
            {
                _activeAxisInputs.Add(positiveInput);
                if (OnControllerInputPressed(positiveInput))
                    anyHandled = true;
            }
            else if (!nowPressed && wasPressed)
            {
                _activeAxisInputs.Remove(positiveInput);
                if (OnControllerInputReleased(positiveInput))
                    anyHandled = true;
            }
        }

        if (_joyAxisMap.TryGetValue(negativeKey, out var negativeInput))
        {
            bool nowPressed = value < -StickDeadzone;
            bool wasPressed = _activeAxisInputs.Contains(negativeInput);

            if (nowPressed && !wasPressed)
            {
                _activeAxisInputs.Add(negativeInput);
                if (OnControllerInputPressed(negativeInput))
                    anyHandled = true;
            }
            else if (!nowPressed && wasPressed)
            {
                _activeAxisInputs.Remove(negativeInput);
                if (OnControllerInputReleased(negativeInput))
                    anyHandled = true;
            }
        }

        return anyHandled;
    }

    /// <summary>
    /// Handle InputEventAction from controller. The game sends both raw controller
    /// actions (controller_d_pad_south) and remapped game actions (ui_down).
    /// We handle the controller_* ones via ControllerInput matching and ignore
    /// the remapped ui_* ones to avoid double-handling.
    /// </summary>
    private static bool OnControllerActionEvent(InputEventAction actionEvent)
    {
        var actionName = actionEvent.Action.ToString();

        // Only handle controller_* actions — the remapped ui_*/mega_* ones would
        // double-fire since we dispatch from the controller_* match.
        if (!_controllerActionMap.TryGetValue(actionName, out var controllerInput))
            return false;

        if (actionEvent.Pressed)
            return OnControllerInputPressed(controllerInput);
        else
            return OnControllerInputReleased(controllerInput);
    }

    private static bool OnControllerInputPressed(ControllerInput input)
    {
        Speech.SpeechManager.Silence();

        bool anyConsumed = false;
        foreach (var action in _actions)
        {
            if (_activeActions.Contains(action))
                continue;

            if (action.MatchesControllerInput(input))
            {
                _activeActions.Add(action);

                if (!anyConsumed)
                {
                    bool consumed = ScreenManager.DispatchAction(action, InputActionState.JustPressed);
                    if (consumed)
                        anyConsumed = true;
                    else if (action.GameAction != null)
                        InjectGameAction(action.GameAction, pressed: true);
                }
            }
        }

        return anyConsumed;
    }

    private static bool OnControllerInputReleased(ControllerInput input)
    {
        var toRelease = new List<InputAction>();
        foreach (var action in _activeActions)
        {
            if (action.MatchesControllerInput(input))
                toRelease.Add(action);
        }

        bool anyConsumed = false;
        foreach (var action in toRelease)
        {
            _activeActions.Remove(action);
            bool consumed = ScreenManager.DispatchAction(action, InputActionState.JustReleased);
            if (!consumed && action.GameAction != null)
                InjectGameAction(action.GameAction, pressed: false);
            if (consumed)
                anyConsumed = true;
        }

        return anyConsumed;
    }

    private static bool IsGameListeningForRebind()
    {
        if (ListeningEntryField == null)
            return false;

        var screen = ActiveScreenContext.Instance.GetCurrentScreen();
        if (screen is not NInputSettingsPanel panel)
            return false;

        return ListeningEntryField.GetValue(panel) != null;
    }

    private static void EnsureFocusMode(InputAction action)
    {
        if (_controllerManager == null)
            return;

        if (!_navActions.Contains(action.Key))
            return;

        bool isUsingController = (bool)IsUsingControllerProp.GetValue(_controllerManager)!;
        if (isUsingController)
            return;

        IsUsingControllerProp.SetValue(_controllerManager, true);

        var viewport = _controllerManager.GetViewport();
        if (viewport != null)
        {
            var mousePos = DisplayServer.MouseGetPosition();
            var windowPos = DisplayServer.WindowGetPosition();
            var localMouse = new Vector2(mousePos.X - windowPos.X, mousePos.Y - windowPos.Y);
            LastMouseField?.SetValue(_controllerManager, localMouse);
            viewport.WarpMouse(Vector2.One * -1000f);
        }

        ActiveScreenContext.Instance.FocusOnDefaultControl();
        _controllerManager.EmitSignal("ControllerDetected");

        Log.Info("[AccessibilityMod] Keyboard nav: switched to focus mode");
    }

    private static void InjectGameAction(string actionName, bool pressed)
    {
        var inputEventAction = new InputEventAction
        {
            Action = actionName,
            Pressed = pressed
        };
        Godot.Input.ParseInputEvent(inputEventAction);
    }
}
