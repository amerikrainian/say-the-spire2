using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace SayTheSpire2.UI;

/// <summary>
/// Reads the secondary display value from powers implementing BaseLib's
/// IHasSecondAmount (a counter or cooldown modded powers show next to their
/// stack count — visible to sighted players as a second number on the power
/// icon). BaseLib is an optional third-party mod, so everything here degrades
/// to null without it.
/// </summary>
public static class PowerSecondAmount
{
    private static readonly System.Type? SecondAmountType =
        AccessTools.TypeByName("BaseLib.Abstracts.IHasSecondAmount");
    private static readonly System.Reflection.MethodInfo? GetSecondAmountMethod =
        SecondAmountType != null ? AccessTools.Method(SecondAmountType, "GetSecondAmount") : null;

    public static string? Get(PowerModel power)
    {
        if (GetSecondAmountMethod == null || SecondAmountType?.IsInstanceOfType(power) != true)
            return null;
        try
        {
            var text = GetSecondAmountMethod.Invoke(power, null) as string;
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Power second amount read failed: {e.Message}");
            return null;
        }
    }
}
