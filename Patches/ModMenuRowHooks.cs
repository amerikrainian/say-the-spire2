using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace SayTheSpire2.Patches;

/// <summary>
/// Makes mod rows toggleable with the keyboard. The game toggles a selected
/// row's tickbox in NModMenuRow._GuiInput via IsActionPressed(accept), but
/// remapped keyboard input arrives as InputEventAction, which Godot never
/// routes into _GuiInput — so the two-step select/toggle interaction only
/// works on a physical controller. We replicate it on OnRelease: a release on
/// an already-selected row toggles its tickbox. Gated to controller mode so
/// mouse clicks keep the game's click-to-reselect behavior.
/// </summary>
public static class ModMenuRowHooks
{
    private static readonly System.Reflection.FieldInfo IsSelectedField =
        AccessTools.Field(typeof(NModMenuRow), "_isSelected")!;
    private static readonly System.Reflection.FieldInfo TickboxField =
        AccessTools.Field(typeof(NModMenuRow), "_tickbox")!;

    // Branch-divergent: the beta exposes the toggle as ForceToggleTick;
    // stable only has it inline in the protected OnRelease. Either invocation
    // flips the tickbox and emits Toggled so the game persists the change.
    private static readonly System.Reflection.MethodInfo? ForceToggleMethod =
        AccessTools.Method(typeof(NTickbox), "ForceToggleTick");
    private static readonly System.Reflection.MethodInfo? TickboxReleaseMethod =
        AccessTools.Method(typeof(NTickbox), "OnRelease");

    public static void Initialize(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(NModMenuRow), "OnRelease");
        if (method == null)
        {
            Log.Error("[AccessibilityMod] Could not find NModMenuRow.OnRelease!");
            return;
        }
        harmony.Patch(method,
            prefix: new HarmonyMethod(typeof(ModMenuRowHooks), nameof(OnReleasePrefix)),
            postfix: new HarmonyMethod(typeof(ModMenuRowHooks), nameof(OnReleasePostfix)));
        Log.Info("[AccessibilityMod] NModMenuRow.OnRelease hook patched.");
    }

    public static void OnReleasePrefix(NModMenuRow __instance, out bool __state)
    {
        __state = IsSelectedField.GetValue(__instance) is true;
    }

    public static void OnReleasePostfix(NModMenuRow __instance, bool __state)
    {
        try
        {
            if (!__state)
                return;
            if (!(NControllerManager.Instance?.IsUsingController ?? false))
                return;

            if (TickboxField.GetValue(__instance) is NTickbox tickbox)
                (ForceToggleMethod ?? TickboxReleaseMethod)?.Invoke(tickbox, null);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Mod row toggle error: {e}");
        }
    }
}
