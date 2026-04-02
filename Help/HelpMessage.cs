using System.Collections.Generic;

namespace SayTheSpire2.Help;

public abstract class HelpMessage
{
    public bool Exclusive { get; }

    protected HelpMessage(bool exclusive = false)
    {
        Exclusive = exclusive;
    }
}

public class TextHelpMessage : HelpMessage
{
    public string Text { get; }

    public TextHelpMessage(string text, bool exclusive = false) : base(exclusive)
    {
        Text = text;
    }
}

public class ControlHelpMessage : HelpMessage
{
    public string Description { get; }
    public List<string> ActionKeys { get; }

    public ControlHelpMessage(string description, string actionKey, bool exclusive = false) : base(exclusive)
    {
        Description = description;
        ActionKeys = new List<string> { actionKey };
    }

    public ControlHelpMessage(string description, string[] actionKeys, bool exclusive = false) : base(exclusive)
    {
        Description = description;
        ActionKeys = new List<string>(actionKeys);
    }
}
