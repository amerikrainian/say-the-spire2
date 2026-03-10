using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Rooms;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Events;

public static class CombatEventManager
{
    private static CombatScreen? _activeCombatScreen;

    public static void Initialize()
    {
        var cm = CombatManager.Instance;
        cm.CombatSetUp += OnCombatSetUp;
        cm.CombatEnded += OnCombatEnded;
        Log.Info("[AccessibilityMod] CombatEventManager initialized.");
    }

    private static void OnCombatSetUp(CombatState state)
    {
        Log.Info($"[EventDebug] CombatEventManager.OnCombatSetUp: existing screen={_activeCombatScreen != null}, creatures={state.Creatures.Count}");
        if (RunScreen.Current == null)
        {
            Log.Error("[AccessibilityMod] CombatSetUp but no RunScreen!");
            return;
        }
        _activeCombatScreen = new CombatScreen();
        RunScreen.Current.PushChild(_activeCombatScreen);
    }

    private static void OnCombatEnded(CombatRoom _)
    {
        Log.Info($"[EventDebug] CombatEventManager.OnCombatEnded: active screen={_activeCombatScreen != null}");
        CleanUp();
    }

    /// <summary>
    /// Remove the active combat screen if one exists. Called both when combat
    /// ends normally and when the run ends (combat may not end cleanly on death/abandon).
    /// </summary>
    public static void CleanUp()
    {
        if (_activeCombatScreen != null)
        {
            ScreenManager.RemoveFromTree(_activeCombatScreen);
            _activeCombatScreen = null;
        }
    }
}
