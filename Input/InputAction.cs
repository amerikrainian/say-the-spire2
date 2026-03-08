using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SayTheSpire2.Input;

public class InputAction
{
    public string Key { get; }
    public string? GameAction { get; }
    private readonly List<InputBinding> _keyBindings = new();
    private readonly List<ControllerInput> _controllerBindings = new();

    public InputAction(string key, string? gameAction = null)
    {
        Key = key;
        GameAction = gameAction;
    }

    public InputAction AddBinding(Godot.Key keycode, bool ctrl = false, bool shift = false, bool alt = false)
    {
        _keyBindings.Add(new InputBinding(keycode, ctrl, shift, alt));
        return this;
    }

    public InputAction AddBinding(ControllerInput input)
    {
        _controllerBindings.Add(input);
        return this;
    }

    /// <summary>
    /// Check if any keyboard binding matches the given key event.
    /// </summary>
    public bool MatchesKeyEvent(InputEventKey key) => _keyBindings.Any(b => b.Matches(key));

    /// <summary>
    /// Check if any keyboard binding uses the given key (for release detection).
    /// </summary>
    public bool UsesKey(Godot.Key keycode) => _keyBindings.Any(b => b.Keycode == keycode);

    /// <summary>
    /// Check if any controller binding matches the given input.
    /// </summary>
    public bool MatchesControllerInput(ControllerInput input) => _controllerBindings.Contains(input);
}
