using System.Collections.Generic;
using System.Linq;
using SayTheSpire2.Input;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Help;

public class HelpScreenBuilder
{
    private readonly List<TextHelpMessage> _textMessages = new();
    private readonly List<ControlHelpMessage> _controlList = new();
    private readonly HashSet<string> _seenActionKeys = new();

    public void AddFromScreenStack()
    {
        bool isFirst = true;
        foreach (var screen in ScreenManager.WalkScreensDeepestFirst())
        {
            foreach (var message in screen.GetHelpMessages())
            {
                if (message.Exclusive && !isFirst)
                    continue;

                switch (message)
                {
                    case TextHelpMessage text:
                        _textMessages.Add(text);
                        break;
                    case ControlHelpMessage control:
                        TryAddControl(control);
                        break;
                }
            }
            isFirst = false;
        }
    }

    public void AddAlwaysPresent()
    {
        TryAddControl(new ControlHelpMessage("Navigate Up", "ui_up"));
        TryAddControl(new ControlHelpMessage("Navigate Down", "ui_down"));
        TryAddControl(new ControlHelpMessage("Navigate Left", "ui_left"));
        TryAddControl(new ControlHelpMessage("Navigate Right", "ui_right"));
        TryAddControl(new ControlHelpMessage("Confirm", "ui_accept"));
        TryAddControl(new ControlHelpMessage("Select", "ui_select"));
        TryAddControl(new ControlHelpMessage("Cancel / Go Back", "ui_cancel"));
        TryAddControl(new ControlHelpMessage("Next Buffer Item", "buffer_next_item"));
        TryAddControl(new ControlHelpMessage("Previous Buffer Item", "buffer_prev_item"));
        TryAddControl(new ControlHelpMessage("Next Buffer", "buffer_next"));
        TryAddControl(new ControlHelpMessage("Previous Buffer", "buffer_prev"));
        TryAddControl(new ControlHelpMessage("Open Mod Menu", "mod_settings"));
        TryAddControl(new ControlHelpMessage("Help", "help"));
    }

    public List<HelpMessage> Build()
    {
        var result = new List<HelpMessage>();
        result.AddRange(_textMessages);
        result.AddRange(_controlList);
        return result;
    }

    public static string? FormatBindings(IEnumerable<string> actionKeys)
    {
        var bindings = new List<string>();
        foreach (var key in actionKeys)
        {
            var action = InputManager.Actions.FirstOrDefault(a => a.Key == key);
            if (action == null) continue;
            foreach (var binding in action.Bindings)
                bindings.Add(binding.DisplayName);
        }
        return bindings.Count > 0 ? string.Join(", ", bindings) : null;
    }

    private void TryAddControl(ControlHelpMessage control)
    {
        // Skip if any of the action keys are already registered
        if (control.ActionKeys.Any(key => _seenActionKeys.Contains(key)))
            return;

        foreach (var key in control.ActionKeys)
            _seenActionKeys.Add(key);
        _controlList.Add(control);
    }
}
