using Godot;

namespace SayTheSpire2.Input;

public class InputBinding
{
    public Key Keycode { get; }
    public bool Ctrl { get; }
    public bool Shift { get; }
    public bool Alt { get; }

    public InputBinding(Key keycode, bool ctrl = false, bool shift = false, bool alt = false)
    {
        Keycode = keycode;
        Ctrl = ctrl;
        Shift = shift;
        Alt = alt;
    }

    public bool Matches(InputEventKey key)
    {
        return key.Keycode == Keycode
            && key.CtrlPressed == Ctrl
            && key.ShiftPressed == Shift
            && key.AltPressed == Alt;
    }
}
