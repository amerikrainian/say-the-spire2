using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Buffers;
using SayTheSpire2.Input;

namespace SayTheSpire2.UI.Screens;

public class DefaultScreen : Screen
{
    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            case "buffer_next_item":
                BufferControls.NextItem();
                return true;
            case "buffer_prev_item":
                BufferControls.PreviousItem();
                return true;
            case "buffer_next":
                BufferControls.NextBuffer();
                return true;
            case "buffer_prev":
                BufferControls.PreviousBuffer();
                return true;
            case "reset_bindings":
                MegaCrit.Sts2.Core.Logging.Log.Info("[AccessibilityMod] Global hotkey: Ctrl+Shift+R - resetting bindings");
                NInputManager.Instance?.ResetToDefaults();
                return true;
        }

        return false;
    }
}
