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
    private static readonly HashSet<ControllerInput> _heldControllerInputs = new();

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
        RegisterCustomInputMapActions();
        RegisterGameActions();
        RegisterModActions();
        Log.Info($"[AccessibilityMod] InputManager initialized with {_actions.Count} actions.");
    }

    /// <summary>
    /// Modify Godot input map so the game doesn't also handle right stick click
    /// (it maps both stick clicks to controller_joystick_press by default).
    /// </summary>
    private static void RegisterCustomInputMapActions()
    {
        try
        {
            if (!InputMap.HasAction("controller_joystick_press")) return;
            foreach (var evt in InputMap.ActionGetEvents("controller_joystick_press"))
            {
                if (evt is InputEventJoypadButton joyEvt && joyEvt.ButtonIndex == JoyButton.RightStick)
                {
                    InputMap.ActionEraseEvent("controller_joystick_press", evt);
                    Log.Info("[AccessibilityMod] Removed RightStick from controller_joystick_press");
                    return;
                }
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Failed to modify controller_joystick_press: {e.Message}");
        }
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
            .AddBinding(ControllerInput.LeftShoulder, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("mega_view_discard_pile", gameAction: "mega_view_discard_pile")
            .AddBinding(Key.S)
            .AddBinding(ControllerInput.RightShoulder, modifier: ControllerInput.RightTrigger));
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
        _actions.Add(new InputAction("announce_hp").AddBinding(Key.H, ctrl: true)
            .AddBinding(ControllerInput.A, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_block").AddBinding(Key.B, ctrl: true)
            .AddBinding(ControllerInput.B, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_energy").AddBinding(Key.Y, ctrl: true)
            .AddBinding(ControllerInput.X, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_powers").AddBinding(Key.P, ctrl: true)
            .AddBinding(ControllerInput.Y, modifier: ControllerInput.LeftTrigger));
        _actions.Add(new InputAction("announce_intents").AddBinding(Key.I, ctrl: true));
        _actions.Add(new InputAction("mod_settings").AddBinding(Key.M, ctrl: true)
            .AddBinding(ControllerInput.Start, modifier: ControllerInput.LeftTrigger));
    }

    /// <summary>
    /// Maps game controller action names to ControllerInput.
    /// Controller events arrive at _UnhandledInput as InputEventAction with these names.
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
    /// Controller buttons polled directly from hardware in _Process, bypassing
    /// the game's input system. For buttons the game doesn't deliver as actions.
    /// </summary>
    private static readonly Dictionary<JoyButton, ControllerInput> _polledButtons = new()
    {
        { JoyButton.RightStick, ControllerInput.RightStickClick },
    };

    private static readonly HashSet<JoyButton> _activePolledButtons = new();

    /// <summary>
    /// Called from _Input prefix on NControllerManager.
    /// Handles keyboard events. Controller events don't arrive here.
    /// </summary>
    public static bool OnInputEvent(NControllerManager controller, InputEvent inputEvent)
    {
        if (!InterceptInput || IsGameListeningForRebind())
            return false;

        _controllerManager = controller;

        if (inputEvent is InputEventKey keyEvent)
        {
            if (keyEvent.Echo)
                return true;

            if (keyEvent.Pressed)
                OnKeyPressed(keyEvent);
            else
                OnKeyReleased(keyEvent);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Called from _UnhandledInput prefix on NInputManager.
    /// Controller InputEventAction events arrive here (not at _Input).
    /// Returns true to consume the event and skip the game's controller remapping.
    /// </summary>
    public static bool OnUnhandledInput(NInputManager inputManager, InputEvent inputEvent)
    {
        if (!InterceptInput || IsGameListeningForRebind())
            return false;

        _controllerManager ??= inputManager.ControllerManager;

        foreach (var (actionName, controllerInput) in _controllerActionMap)
        {
            if (inputEvent.IsActionPressed(actionName))
            {
                OnControllerInputPressed(controllerInput);
                return true;
            }
            else if (inputEvent.IsActionReleased(actionName))
            {
                OnControllerInputReleased(controllerInput);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Called from _Process postfix on NControllerManager. Polls raw hardware state
    /// for controller buttons that the game's input system doesn't deliver.
    /// </summary>
    public static void PollCustomActions(NControllerManager controller)
    {
        if (!InterceptInput || IsGameListeningForRebind())
            return;

        _controllerManager = controller;

        foreach (var (button, controllerInput) in _polledButtons)
        {
            bool isPressed = false;
            foreach (int device in Godot.Input.GetConnectedJoypads())
            {
                if (Godot.Input.IsJoyButtonPressed(device, button))
                {
                    isPressed = true;
                    break;
                }
            }

            bool wasPressed = _activePolledButtons.Contains(button);

            if (isPressed && !wasPressed)
            {
                _activePolledButtons.Add(button);
                OnControllerInputPressed(controllerInput);
            }
            else if (!isPressed && wasPressed)
            {
                _activePolledButtons.Remove(button);
                OnControllerInputReleased(controllerInput);
            }
        }
    }

    private static void OnKeyPressed(InputEventKey keyEvent)
    {
        Speech.SpeechManager.Silence();

        if (_modifierKeys.Contains(keyEvent.Keycode))
            return;

        bool anyConsumed = false;
        foreach (var action in _actions)
        {
            if (_activeActions.Contains(action))
                continue;

            if (action.MatchesKeyEvent(keyEvent))
            {
                _activeActions.Add(action);
                EnsureFocusMode(action);

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

    private static bool IsControllerInputHeld(ControllerInput input) => _heldControllerInputs.Contains(input);

    private static bool OnControllerInputPressed(ControllerInput input)
    {
        Speech.SpeechManager.Silence();
        _heldControllerInputs.Add(input);

        bool anyConsumed = false;
        InputAction? unmatchedFallback = null;

        foreach (var action in _actions)
        {
            if (_activeActions.Contains(action))
                continue;

            if (action.MatchesControllerInput(input, IsControllerInputHeld))
            {
                if (action.HasControllerModifier)
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
                    unmatchedFallback = null;
                    break;
                }
                else if (unmatchedFallback == null)
                {
                    unmatchedFallback = action;
                }
            }
        }

        if (unmatchedFallback != null)
        {
            _activeActions.Add(unmatchedFallback);
            if (!anyConsumed)
            {
                bool consumed = ScreenManager.DispatchAction(unmatchedFallback, InputActionState.JustPressed);
                if (consumed)
                    anyConsumed = true;
                else if (unmatchedFallback.GameAction != null)
                    InjectGameAction(unmatchedFallback.GameAction, pressed: true);
            }
        }

        return anyConsumed;
    }

    private static bool OnControllerInputReleased(ControllerInput input)
    {
        _heldControllerInputs.Remove(input);

        var toRelease = new List<InputAction>();
        foreach (var action in _activeActions)
        {
            if (action.UsesControllerInput(input))
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
