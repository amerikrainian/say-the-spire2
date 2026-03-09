namespace SayTheSpire2.Input;

public abstract class InputBinding
{
    public abstract string Serialize();
    public abstract string Type { get; }
    public abstract string TypeLabel { get; }
    public abstract string ComboName { get; }
    public string DisplayName => $"{TypeLabel}: {ComboName}";

    public static InputBinding? Deserialize(string type, string binding)
    {
        return type switch
        {
            "keyboard" => KeyboardBinding.Parse(binding),
            "controller" => ControllerBinding.Parse(binding),
            _ => null
        };
    }
}
