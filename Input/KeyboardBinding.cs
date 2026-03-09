using System;
using Godot;

namespace SayTheSpire2.Input;

public class KeyboardBinding : InputBinding
{
    public override string Type => "keyboard";
    public Key Keycode { get; }
    public bool Ctrl { get; }
    public bool Shift { get; }
    public bool Alt { get; }

    public KeyboardBinding(Key keycode, bool ctrl = false, bool shift = false, bool alt = false)
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

    public override string TypeLabel => "Keyboard";
    public override string ComboName => Serialize();

    public override string Serialize()
    {
        var parts = "";
        if (Ctrl) parts += "Ctrl+";
        if (Shift) parts += "Shift+";
        if (Alt) parts += "Alt+";
        return parts + Keycode.ToString();
    }

    public static KeyboardBinding? Parse(string s)
    {
        bool ctrl = false, shift = false, alt = false;
        var remaining = s;

        while (remaining.Contains('+'))
        {
            var idx = remaining.IndexOf('+');
            var mod = remaining.Substring(0, idx);
            remaining = remaining.Substring(idx + 1);

            switch (mod)
            {
                case "Ctrl": ctrl = true; break;
                case "Shift": shift = true; break;
                case "Alt": alt = true; break;
            }
        }

        if (Enum.TryParse<Key>(remaining, out var key))
            return new KeyboardBinding(key, ctrl, shift, alt);
        return null;
    }
}
