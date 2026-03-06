using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace Sts2AccessibilityMod.Input;

public static class InputManager
{
    private static readonly List<InputContext> _contextStack = new();
    private static readonly List<InputAction> _actions = new();

    public static void Initialize()
    {
        _actions.Add(new InputAction("buffer_next_item").AddBinding(Key.Down, ctrl: true));
        _actions.Add(new InputAction("buffer_prev_item").AddBinding(Key.Up, ctrl: true));
        _actions.Add(new InputAction("buffer_next").AddBinding(Key.Right, ctrl: true));
        _actions.Add(new InputAction("buffer_prev").AddBinding(Key.Left, ctrl: true));
        _actions.Add(new InputAction("reset_bindings").AddBinding(Key.R, ctrl: true, shift: true));

        PushContext(new DefaultInputContext());
        Log.Info("[AccessibilityMod] InputManager initialized.");
    }

    /// <summary>
    /// Try to handle a key event. Returns true if the event was consumed by a mod action
    /// (and should NOT be passed to the game). Returns false if it's not a mod action
    /// or all contexts allowed it to pass through.
    /// </summary>
    public static bool HandleKeyEvent(InputEventKey key)
    {
        var action = _actions.FirstOrDefault(a => a.Matches(key));
        if (action == null)
            return false; // Not a mod action — let game handle it

        bool consumed = false;

        if (key.Pressed && !key.Echo)
        {
            consumed = DispatchJustPressed(action);
        }
        else if (key.Pressed && key.Echo)
        {
            consumed = DispatchPressed(action);
        }
        else if (!key.Pressed)
        {
            consumed = DispatchJustReleased(action);
        }

        // Even if no context consumed the action, it's still a mod action binding.
        // Only pass through to the game if ALL contexts explicitly allowed it (none consumed).
        return consumed;
    }

    public static void PushContext(InputContext context)
    {
        if (_contextStack.Count > 0)
            _contextStack[^1].OnUnfocus();

        _contextStack.Add(context);
        context.OnPush();
        Log.Info($"[AccessibilityMod] InputContext pushed: {context.GetType().Name} (stack depth: {_contextStack.Count})");
    }

    public static void PopContext()
    {
        if (_contextStack.Count <= 1)
        {
            Log.Error("[AccessibilityMod] Cannot pop the last InputContext!");
            return;
        }

        var context = _contextStack[^1];
        _contextStack.RemoveAt(_contextStack.Count - 1);
        context.OnPop();

        if (_contextStack.Count > 0)
            _contextStack[^1].OnFocus();

        Log.Info($"[AccessibilityMod] InputContext popped: {context.GetType().Name} (stack depth: {_contextStack.Count})");
    }

    public static InputContext? CurrentContext =>
        _contextStack.Count > 0 ? _contextStack[^1] : null;

    private static bool DispatchJustPressed(InputAction action)
    {
        for (int i = _contextStack.Count - 1; i >= 0; i--)
        {
            if (_contextStack[i].OnActionJustPressed(action))
                return true;
        }
        return false;
    }

    private static bool DispatchPressed(InputAction action)
    {
        for (int i = _contextStack.Count - 1; i >= 0; i--)
        {
            if (_contextStack[i].OnActionPressed(action))
                return true;
        }
        return false;
    }

    private static bool DispatchJustReleased(InputAction action)
    {
        for (int i = _contextStack.Count - 1; i >= 0; i--)
        {
            if (_contextStack[i].OnActionJustReleased(action))
                return true;
        }
        return false;
    }
}
