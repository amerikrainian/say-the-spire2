using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SayTheSpire2.Input;

public class InputAction
{
    public string Key { get; }
    public string Label { get; }
    public string? GameAction { get; }
    private readonly List<InputBinding> _bindings = new();

    public IReadOnlyList<InputBinding> Bindings => _bindings;

    public event Action? BindingsChanged;

    public InputAction(string key, string label, string? gameAction = null)
    {
        Key = key;
        Label = label;
        GameAction = gameAction;
    }

    public InputAction AddBinding(InputBinding binding)
    {
        _bindings.Add(binding);
        BindingsChanged?.Invoke();
        return this;
    }

    public InputAction AddBinding(Godot.Key keycode, bool ctrl = false, bool shift = false, bool alt = false)
    {
        _bindings.Add(new KeyboardBinding(keycode, ctrl, shift, alt));
        BindingsChanged?.Invoke();
        return this;
    }

    public InputAction AddBinding(ControllerInput input, ControllerInput? modifier = null)
    {
        _bindings.Add(new ControllerBinding(input, modifier));
        BindingsChanged?.Invoke();
        return this;
    }

    public void RemoveBinding(InputBinding binding)
    {
        _bindings.Remove(binding);
        BindingsChanged?.Invoke();
    }

    public void ClearBindings()
    {
        _bindings.Clear();
        BindingsChanged?.Invoke();
    }

    /// <summary>
    /// Check if any keyboard binding matches the given key event.
    /// </summary>
    public bool MatchesKeyEvent(InputEventKey key) => _bindings.OfType<KeyboardBinding>().Any(b => b.Matches(key));

    /// <summary>
    /// Check if any keyboard binding uses the given key (for release detection).
    /// </summary>
    public bool UsesKey(Godot.Key keycode) => _bindings.OfType<KeyboardBinding>().Any(b => b.Keycode == keycode);

    /// <summary>
    /// Check if any controller binding matches the given input, considering held modifiers.
    /// </summary>
    public bool MatchesControllerInput(ControllerInput input, Func<ControllerInput, bool> isHeld)
        => _bindings.OfType<ControllerBinding>().Any(b => b.Matches(input, isHeld));

    /// <summary>
    /// Check if any controller binding uses the given input (primary or modifier) for release detection.
    /// </summary>
    public bool UsesControllerInput(ControllerInput input)
        => _bindings.OfType<ControllerBinding>().Any(b => b.Uses(input));

    /// <summary>
    /// Whether any controller binding requires a modifier.
    /// Used to prioritize modified bindings over unmodified ones.
    /// </summary>
    public bool HasControllerModifier => _bindings.OfType<ControllerBinding>().Any(b => b.Modifier != null);
}
