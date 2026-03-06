using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using Sts2AccessibilityMod.Buffers;
using Sts2AccessibilityMod.Speech;
using Sts2AccessibilityMod.UI;

namespace Sts2AccessibilityMod.Hooks;

public static class FocusHooks
{
    private static readonly PropertyInfo IsFocusedProp =
        typeof(NClickableControl).GetProperty("IsFocused", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static Control? _lastAnnouncedControl;

    public static void Initialize(Harmony harmony)
    {
        // Connect to Viewport focus changes to catch ALL focus events (including non-NClickableControl)
        ConnectViewportFocusWatcher();

        // Patch NClickableControl.RefreshFocus for buttons, tickboxes, dropdowns, etc.
        var refreshFocus = AccessTools.Method(typeof(NClickableControl), "RefreshFocus");
        if (refreshFocus == null)
        {
            Log.Error("[AccessibilityMod] Could not find RefreshFocus method!");
            return;
        }

        var prefix = new HarmonyMethod(typeof(FocusHooks).GetMethod(nameof(RefreshFocusPrefix), BindingFlags.Static | BindingFlags.Public));
        var postfix = new HarmonyMethod(typeof(FocusHooks).GetMethod(nameof(RefreshFocusPostfix), BindingFlags.Static | BindingFlags.Public));
        harmony.Patch(refreshFocus, prefix: prefix, postfix: postfix);
        Log.Info("[AccessibilityMod] RefreshFocus hook patched.");

        // Patch NSettingsSlider.OnFocus and NPaginator.OnFocus (not NClickableControl subclasses)
        PatchOnFocus<NSettingsSlider>(harmony, nameof(SettingsControlFocusPostfix), "Slider");
        PatchOnFocus<NPaginator>(harmony, nameof(SettingsControlFocusPostfix), "Paginator");

        // Patch combat focus: card holders and creatures have their own focus systems
        PatchOnFocus<NHandCardHolder>(harmony, nameof(CardHolderFocusPostfix), "HandCardHolder");
        PatchOnFocus<NGridCardHolder>(harmony, nameof(CardHolderFocusPostfix), "GridCardHolder");

        var creatureOnFocus = AccessTools.Method(typeof(NCreature), "OnFocus");
        if (creatureOnFocus != null)
        {
            harmony.Patch(creatureOnFocus,
                postfix: new HarmonyMethod(typeof(FocusHooks), nameof(CreatureFocusPostfix)));
            Log.Info("[AccessibilityMod] Creature focus hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NCreature.OnFocus()!");
        }
    }

    public static void RefreshFocusPrefix(NClickableControl __instance, out bool __state)
    {
        __state = (bool)IsFocusedProp.GetValue(__instance)!;
    }

    public static void RefreshFocusPostfix(NClickableControl __instance, bool __state)
    {
        bool nowFocused = (bool)IsFocusedProp.GetValue(__instance)!;
        if (nowFocused && !__state)
        {
            AnnounceElement(__instance);
        }
    }

    public static void SettingsControlFocusPostfix(Control __instance)
    {
        AnnounceElement(__instance);
    }

    public static void CardHolderFocusPostfix(NCardHolder __instance)
    {
        AnnounceElement(__instance, new ProxyCard(__instance));
    }

    public static void CreatureFocusPostfix(NCreature __instance)
    {
        AnnounceElement(__instance, new ProxyCreature(__instance));
    }

    private static void PatchOnFocus<T>(Harmony harmony, string postfixMethodName, string label)
    {
        var onFocus = AccessTools.Method(typeof(T), "OnFocus");
        if (onFocus != null)
        {
            var postfix = new HarmonyMethod(typeof(FocusHooks).GetMethod(postfixMethodName, BindingFlags.Static | BindingFlags.Public));
            harmony.Patch(onFocus, postfix: postfix);
            Log.Info($"[AccessibilityMod] {label} focus hook patched.");
        }
        else
        {
            Log.Error($"[AccessibilityMod] Could not find {typeof(T).Name}.OnFocus()!");
        }
    }

    private static void ConnectViewportFocusWatcher()
    {
        // Deferred connection since viewport may not be ready during Initialize
        Callable.From(() =>
        {
            try
            {
                var viewport = ((SceneTree)Engine.GetMainLoop()).Root;
                viewport.Connect("gui_focus_changed", Callable.From<Control>(OnViewportFocusChanged));
                Log.Info("[AccessibilityMod] Viewport focus watcher connected.");
            }
            catch (System.Exception e)
            {
                Log.Error($"[AccessibilityMod] Failed to connect viewport focus watcher: {e.Message}");
            }
        }).CallDeferred();
    }

    private static void OnViewportFocusChanged(Control control)
    {
        // Skip NClickableControl - those are already handled by RefreshFocus hook
        if (control is NClickableControl) return;
        // Skip card holders and creatures - handled by their own hooks
        if (control is NCardHolder) return;
        if (control is NCreature) return;
        // Skip if we just announced this control
        if (control == _lastAnnouncedControl) return;
        // CardHolderContainer is a transient focus target - focus shifts away immediately
        if (control.Name == "CardHolderContainer") return;

        Log.Info($"[AccessibilityMod] Viewport focus (non-NClickable): {control.GetType().FullName} ({control.Name})");
        AnnounceElement(control);
    }

    private static void AnnounceElement(Control control, UIElement? preResolved = null)
    {
        _lastAnnouncedControl = control;
        var element = preResolved ?? ResolveElement(control);
        var text = element.GetFocusString();
        Log.Info($"[AccessibilityMod] Focus: {control.GetType().Name} ({control.Name}) -> \"{text}\"");
        if (!string.IsNullOrEmpty(text))
        {
            SpeechManager.Output(text);
        }

        // Update buffers for the focused element
        var buffers = BufferManager.Instance;
        buffers.DisableAll();
        var currentBufferKey = element.HandleBuffers(buffers);
        if (currentBufferKey != null)
            buffers.SetCurrentBuffer(currentBufferKey);
    }

    private static UIElement ResolveElement(Control control)
    {
        var screenElement = GameScreenManager.ActiveScreen?.GetElement(control);
        if (screenElement != null)
            return screenElement;

        return ProxyFactory.Create(control);
    }
}
