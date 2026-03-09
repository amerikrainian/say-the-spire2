namespace SayTheSpire2.Input;

public class ControllerBinding
{
    public ControllerInput Input { get; }
    public ControllerInput? Modifier { get; }

    public ControllerBinding(ControllerInput input, ControllerInput? modifier = null)
    {
        Input = input;
        Modifier = modifier;
    }

    public bool Matches(ControllerInput input, System.Func<ControllerInput, bool> isHeld)
    {
        if (input != Input)
            return false;
        if (Modifier != null && !isHeld(Modifier.Value))
            return false;
        if (Modifier == null)
            return true;
        return true;
    }

    /// <summary>
    /// Whether this binding uses the given input as its primary or modifier.
    /// Used for release detection.
    /// </summary>
    public bool Uses(ControllerInput input) => Input == input || Modifier == input;
}
