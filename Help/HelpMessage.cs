using System.Collections.Generic;

namespace SayTheSpire2.Help;

public abstract class HelpMessage { }

public class TextHelpMessage : HelpMessage
{
    public string Text { get; }

    public TextHelpMessage(string text)
    {
        Text = text;
    }
}

public class ControlHelpMessage : HelpMessage
{
    public string Description { get; }
    public List<string> ActionKeys { get; }

    public ControlHelpMessage(string description, string actionKey)
    {
        Description = description;
        ActionKeys = new List<string> { actionKey };
    }

    public ControlHelpMessage(string description, params string[] actionKeys)
    {
        Description = description;
        ActionKeys = new List<string>(actionKeys);
    }
}
