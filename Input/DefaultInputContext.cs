using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Sts2AccessibilityMod.Buffers;

namespace Sts2AccessibilityMod.Input;

public class DefaultInputContext : InputContext
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
