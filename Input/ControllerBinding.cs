using System;

namespace SayTheSpire2.Input;

public class ControllerBinding : InputBinding
{
    public override string Type => "controller";
    public ControllerInput Input { get; }
    public ControllerInput? Modifier { get; }

    public ControllerBinding(ControllerInput input, ControllerInput? modifier = null)
    {
        Input = input;
        Modifier = modifier;
    }

    public bool Matches(ControllerInput input, Func<ControllerInput, bool> isHeld)
    {
        if (input != Input)
            return false;
        if (Modifier != null && !isHeld(Modifier.Value))
            return false;
        return true;
    }

    /// <summary>
    /// Whether this binding uses the given input as its primary or modifier.
    /// Used for release detection.
    /// </summary>
    public bool Uses(ControllerInput input) => Input == input || Modifier == input;

    public override string TypeLabel => "Controller";
    public override string ComboName
    {
        get
        {
            var name = GetDisplayName(Input);
            if (Modifier != null)
                return GetDisplayName(Modifier.Value) + "+" + name;
            return name;
        }
    }

    public static string GetDisplayName(ControllerInput input) => input switch
    {
        ControllerInput.DpadUp => "D-pad Up",
        ControllerInput.DpadDown => "D-pad Down",
        ControllerInput.DpadLeft => "D-pad Left",
        ControllerInput.DpadRight => "D-pad Right",
        ControllerInput.LeftShoulder => "LB",
        ControllerInput.RightShoulder => "RB",
        ControllerInput.LeftTrigger => "LT",
        ControllerInput.RightTrigger => "RT",
        ControllerInput.LeftStickUp => "LS Up",
        ControllerInput.LeftStickDown => "LS Down",
        ControllerInput.LeftStickLeft => "LS Left",
        ControllerInput.LeftStickRight => "LS Right",
        ControllerInput.RightStickUp => "RS Up",
        ControllerInput.RightStickDown => "RS Down",
        ControllerInput.RightStickLeft => "RS Left",
        ControllerInput.RightStickRight => "RS Right",
        ControllerInput.LeftStickClick => "LS Click",
        ControllerInput.RightStickClick => "RS Click",
        _ => input.ToString()
    };

    public override string Serialize()
    {
        if (Modifier != null)
            return Modifier.Value.ToString() + "+" + Input.ToString();
        return Input.ToString();
    }

    public static ControllerBinding? Parse(string s)
    {
        if (s.Contains('+'))
        {
            var idx = s.IndexOf('+');
            var modStr = s.Substring(0, idx);
            var inputStr = s.Substring(idx + 1);

            if (Enum.TryParse<ControllerInput>(modStr, out var mod) &&
                Enum.TryParse<ControllerInput>(inputStr, out var input))
                return new ControllerBinding(input, mod);
            return null;
        }

        if (Enum.TryParse<ControllerInput>(s, out var solo))
            return new ControllerBinding(solo);
        return null;
    }
}
