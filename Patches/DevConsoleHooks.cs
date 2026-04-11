using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.Patches;

public static class DevConsoleHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(DevConsole), "ProcessCommand",
            typeof(DevConsoleHooks), nameof(ProcessCommandPostfix), "DevConsole ProcessCommand",
            parameterTypes: new[] { typeof(string) });
    }

    public static void ProcessCommandPostfix(CmdResult __result)
    {
        try
        {
            if (!string.IsNullOrEmpty(__result.msg))
            {
                var text = ProxyElement.StripBbcode(__result.msg);
                if (!string.IsNullOrEmpty(text))
                    SpeechManager.Output(text);
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] DevConsole output speech failed: {e.Message}");
        }
    }
}
