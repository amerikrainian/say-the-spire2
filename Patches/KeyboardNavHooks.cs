using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Input;

namespace SayTheSpire2.Patches;

/// <summary>
/// Patches NControllerManager._Input to intercept keyboard input for the mod's
/// input system, and NInputManager._UnhandledKeyInput to suppress the game's
/// default key-to-action remapping when the mod is intercepting.
/// </summary>
public static class KeyboardNavHooks
{
    public static void Initialize(Harmony harmony)
    {
        var inputMethod = AccessTools.Method(typeof(NControllerManager), "_Input");
        if (inputMethod != null)
        {
            harmony.Patch(inputMethod,
                prefix: new HarmonyMethod(typeof(KeyboardNavHooks).GetMethod(
                    nameof(InputPrefix), BindingFlags.Static | BindingFlags.Public)));
            Log.Info("[AccessibilityMod] NControllerManager._Input hook patched.");
        }

        var unhandledKeyMethod = AccessTools.Method(typeof(NInputManager), "_UnhandledKeyInput");
        if (unhandledKeyMethod != null)
        {
            harmony.Patch(unhandledKeyMethod,
                prefix: new HarmonyMethod(typeof(KeyboardNavHooks).GetMethod(
                    nameof(UnhandledKeyInputPrefix), BindingFlags.Static | BindingFlags.Public)));
            Log.Info("[AccessibilityMod] NInputManager._UnhandledKeyInput suppression patched.");
        }

        var unhandledInputMethod = AccessTools.Method(typeof(NInputManager), "_UnhandledInput");
        if (unhandledInputMethod != null)
        {
            harmony.Patch(unhandledInputMethod,
                prefix: new HarmonyMethod(typeof(KeyboardNavHooks).GetMethod(
                    nameof(UnhandledInputPrefix), BindingFlags.Static | BindingFlags.Public)));
            Log.Info("[AccessibilityMod] NInputManager._UnhandledInput suppression patched.");
        }

        Log.Info("[AccessibilityMod] Input hooks patched.");
    }

    /// <summary>
    /// Intercept all input events on NControllerManager. Keyboard events are
    /// captured and processed immediately; non-keyboard events pass through.
    /// </summary>
    public static bool InputPrefix(NControllerManager __instance, InputEvent inputEvent)
    {
        if (InputManager.OnInputEvent(__instance, inputEvent))
        {
            __instance.GetViewport()?.SetInputAsHandled();
            return false;
        }
        return true;
    }

    /// <summary>
    /// Suppress the game's own key-to-action remapping when the mod is intercepting input.
    /// </summary>
    public static bool UnhandledKeyInputPrefix()
    {
        return !InputManager.InterceptInput;
    }

    /// <summary>
    /// Suppress the game's controller-to-action remapping when the mod is intercepting input.
    /// Without this, the game's NInputManager would also process controller actions and
    /// inject duplicate game actions.
    /// </summary>
    public static bool UnhandledInputPrefix()
    {
        return !InputManager.InterceptInput;
    }
}
