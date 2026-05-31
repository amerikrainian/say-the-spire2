using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.TopBar;

namespace SayTheSpire2.Patches;

/// <summary>
/// Harmony prefix on <c>NTopBarPauseButton.OnRelease</c> to redirect Esc /
/// pause presses to cancel-card-play when a confirmation is active.
///
/// Why patch this instead of intercepting Esc in our action dispatcher:
/// recent betas route Esc through Godot's <c>_UnhandledInput</c> via the
/// game's <c>NHotkeyManager</c>, which fires <c>NTopBarPauseButton</c>'s
/// hotkey handler (registered for <c>MegaInput.pauseAndBack</c>) directly.
/// That channel runs in parallel to our <c>ScreenManager.DispatchAction</c>
/// path and can't be claimed via <c>Screen.ClaimAction</c>. Also,
/// <c>NControllerCardPlay._Input</c> only listens for <c>MegaInput.cancel</c>
/// / <c>topPanel</c>, not <c>pauseAndBack</c>, so the game itself never
/// cancels card-play from an Esc press — it just opens the pause menu on
/// top of the confirmation, leaving the player stuck. Patching the
/// chokepoint is the only reliable way in.
/// </summary>
public static class PauseButtonHooks
{
    private static readonly FieldInfo? CurrentCardPlayField =
        AccessTools.Field(typeof(NPlayerHand), "_currentCardPlay");

    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony,
            typeof(NTopBarPauseButton), "OnRelease",
            typeof(PauseButtonHooks), nameof(OnReleasePrefix),
            "PauseButton.OnRelease", isPrefix: true);
    }

    /// <summary>
    /// Skip the pause-menu open and cancel the active card-play instead
    /// when one is in progress. Returning <c>false</c> from a Harmony
    /// prefix suppresses the original method.
    /// </summary>
    public static bool OnReleasePrefix()
    {
        var combatRoom = NCombatRoom.Instance;
        var hand = combatRoom?.Ui?.Hand;
        if (hand == null || !hand.InCardPlay) return true;

        if (CurrentCardPlayField?.GetValue(hand) is not NCardPlay cardPlay
            || !Godot.GodotObject.IsInstanceValid(cardPlay)) return true;

        // Mirror what the MultiCreatureTargeting Canceled-signal handler
        // does (NControllerCardPlay.cs around line 250-254).
        combatRoom!.EnableControllerNavigation();
        cardPlay.CancelPlayCard();
        return false;
    }
}
