using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SayTheSpire2.Input;

public class InputAction
{
    public string Key { get; }
    private readonly List<InputBinding> _bindings = new();

    public InputAction(string key)
    {
        Key = key;
    }

    public InputAction AddBinding(Godot.Key keycode, bool ctrl = false, bool shift = false, bool alt = false)
    {
        _bindings.Add(new InputBinding(keycode, ctrl, shift, alt));
        return this;
    }

    public bool Matches(InputEventKey key) => _bindings.Any(b => b.Matches(key));
}
