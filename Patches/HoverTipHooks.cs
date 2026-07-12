using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace SayTheSpire2.Patches;

/// <summary>
/// Guards focus flow against a game bug: NHoverTipSet.Init can throw
/// ObjectDisposedException (disposed AtlasTexture) while rendering card/relic
/// images inside hover tips — seen on ancient event relic options like Tanx's
/// Whistle and Touch of Orobas, and reachable from any control whose OnFocus
/// shows such a tip (relic holders included). The exception unwinds through
/// RefreshFocus before our focus postfix runs, so the control announces
/// nothing at all. Suppressing it here lets focus complete — the tip visual
/// is already broken for sighted players either way, and our announcements
/// read from the models, not the tip textures.
/// </summary>
public static class HoverTipHooks
{
    public static void Initialize(Harmony harmony)
    {
        var method = AccessTools.Method(typeof(NHoverTipSet), "Init");
        if (method == null)
        {
            Log.Error("[AccessibilityMod] Could not find NHoverTipSet.Init!");
            return;
        }
        harmony.Patch(method,
            finalizer: new HarmonyMethod(typeof(HoverTipHooks), nameof(InitFinalizer)));
        Log.Info("[AccessibilityMod] NHoverTipSet.Init crash guard patched.");
    }

    public static Exception? InitFinalizer(Exception? __exception)
    {
        if (__exception is ObjectDisposedException)
        {
            Log.Info($"[AccessibilityMod] Suppressed hover tip render crash (game bug): {__exception.Message}");
            return null;
        }
        return __exception;
    }
}
