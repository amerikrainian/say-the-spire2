using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SayTheSpire2.Input;

public class InputAction
{
    public string Key { get; }
    public string? GameAction { get; }
    private readonly List<InputBinding> _bindings = new();

    public InputAction(string key, string? gameAction = null)
    {
        Key = key;
        GameAction = gameAction;
    }

    public InputAction AddBinding(Godot.Key keycode, bool ctrl = false, bool shift = false, bool alt = false)
    {
        _bindings.Add(new InputBinding(keycode, ctrl, shift, alt));
        return this;
    }

    /// <summary>
    /// Check if any binding matches the given key event (keycode + modifiers).
    /// </summary>
    public bool MatchesKeyEvent(InputEventKey key) => _bindings.Any(b => b.Matches(key));

    /// <summary>
    /// Check if any binding uses the given key (for release detection).
    /// </summary>
    public bool UsesKey(Godot.Key keycode) => _bindings.Any(b => b.Keycode == keycode);
}
