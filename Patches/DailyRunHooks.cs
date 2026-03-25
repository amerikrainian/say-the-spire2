using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class DailyRunHooks
{
    public static void Initialize(Harmony harmony)
    {
        PatchIfFound(harmony, typeof(NDailyRunScreen), "OnSubmenuOpened",
            nameof(DailyRunOpenedPostfix), "DailyRun OnSubmenuOpened");
        PatchIfFound(harmony, typeof(NDailyRunScreen), "OnSubmenuClosed",
            nameof(DailyRunClosedPostfix), "DailyRun OnSubmenuClosed");
        PatchIfFound(harmony, typeof(NDailyRunScreen), "CleanUpLobby",
            nameof(DailyRunClosedPostfix), "DailyRun CleanUpLobby");
        PatchIfFound(harmony, typeof(NDailyRunScreen), "PlayerConnected",
            nameof(DailyRunPlayerConnectedPostfix), "DailyRun PlayerConnected");
        PatchIfFound(harmony, typeof(NDailyRunScreen), "PlayerChanged",
            nameof(DailyRunPlayerChangedPostfix), "DailyRun PlayerChanged");
        PatchIfFound(harmony, typeof(NDailyRunScreen), "RemotePlayerDisconnected",
            nameof(DailyRunPlayerDisconnectedPostfix), "DailyRun RemotePlayerDisconnected");
        PatchIfFound(harmony, typeof(NDailyRunScreen), "LocalPlayerDisconnected",
            nameof(DailyRunLocalDisconnectedPostfix), "DailyRun LocalPlayerDisconnected");
        PatchIfFound(harmony, typeof(NDailyRunScreen), "OnEmbarkPressed",
            nameof(DailyRunEmbarkPostfix), "DailyRun OnEmbarkPressed");
        PatchIfFound(harmony, typeof(NDailyRunScreen), "OnUnreadyPressed",
            nameof(DailyRunUnreadyPostfix), "DailyRun OnUnreadyPressed");

        PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "OnSubmenuOpened",
            nameof(DailyRunLoadOpenedPostfix), "DailyRunLoad OnSubmenuOpened");
        PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "OnSubmenuClosed",
            nameof(DailyRunLoadClosedPostfix), "DailyRunLoad OnSubmenuClosed");
        PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "CleanUpLobby",
            nameof(DailyRunLoadClosedPostfix), "DailyRunLoad CleanUpLobby");
        PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "PlayerConnected",
            nameof(DailyRunLoadPlayerConnectedPostfix), "DailyRunLoad PlayerConnected");
        PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "PlayerReadyChanged",
            nameof(DailyRunLoadPlayerReadyChangedPostfix), "DailyRunLoad PlayerReadyChanged");
        PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "RemotePlayerDisconnected",
            nameof(DailyRunLoadPlayerDisconnectedPostfix), "DailyRunLoad RemotePlayerDisconnected");
        PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "LocalPlayerDisconnected",
            nameof(DailyRunLoadLocalDisconnectedPostfix), "DailyRunLoad LocalPlayerDisconnected");
        PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "OnEmbarkPressed",
            nameof(DailyRunLoadEmbarkPostfix), "DailyRunLoad OnEmbarkPressed");
        PatchIfFound(harmony, typeof(NDailyRunLoadScreen), "OnUnreadyPressed",
            nameof(DailyRunLoadUnreadyPostfix), "DailyRunLoad OnUnreadyPressed");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(DailyRunHooks), handlerName, label, isPrefix);
    }

    public static void DailyRunOpenedPostfix(NDailyRunScreen __instance)
    {
        if (DailyRunGameScreen.Current == null)
            ScreenManager.PushScreen(new DailyRunGameScreen(__instance));
    }

    public static void DailyRunClosedPostfix()
    {
        if (DailyRunGameScreen.Current != null)
            ScreenManager.RemoveScreen(DailyRunGameScreen.Current);
    }

    public static void DailyRunPlayerConnectedPostfix(LobbyPlayer player)
    {
        DailyRunGameScreen.Current?.OnPlayerConnected(player);
    }

    public static void DailyRunPlayerChangedPostfix(LobbyPlayer player)
    {
        DailyRunGameScreen.Current?.OnPlayerChanged(player);
    }

    public static void DailyRunPlayerDisconnectedPostfix(LobbyPlayer player)
    {
        DailyRunGameScreen.Current?.OnPlayerDisconnected(player);
    }

    public static void DailyRunLocalDisconnectedPostfix(NetErrorInfo info)
    {
        DailyRunGameScreen.Current?.OnLocalDisconnected();
    }

    public static void DailyRunEmbarkPostfix()
    {
        DailyRunGameScreen.Current?.OnEmbarkPressed();
    }

    public static void DailyRunUnreadyPostfix()
    {
        DailyRunGameScreen.Current?.OnUnreadyPressed();
    }

    public static void DailyRunLoadOpenedPostfix(NDailyRunLoadScreen __instance)
    {
        if (DailyRunLoadGameScreen.Current == null)
            ScreenManager.PushScreen(new DailyRunLoadGameScreen(__instance));
    }

    public static void DailyRunLoadClosedPostfix()
    {
        if (DailyRunLoadGameScreen.Current != null)
            ScreenManager.RemoveScreen(DailyRunLoadGameScreen.Current);
    }

    public static void DailyRunLoadPlayerConnectedPostfix(ulong playerId)
    {
        DailyRunLoadGameScreen.Current?.OnPlayerConnected(playerId);
    }

    public static void DailyRunLoadPlayerReadyChangedPostfix(ulong playerId)
    {
        DailyRunLoadGameScreen.Current?.OnPlayerReadyChanged(playerId);
    }

    public static void DailyRunLoadPlayerDisconnectedPostfix(ulong playerId)
    {
        DailyRunLoadGameScreen.Current?.OnPlayerDisconnected(playerId);
    }

    public static void DailyRunLoadLocalDisconnectedPostfix(NetErrorInfo info)
    {
        DailyRunLoadGameScreen.Current?.OnLocalDisconnected();
    }

    public static void DailyRunLoadEmbarkPostfix()
    {
        DailyRunLoadGameScreen.Current?.OnEmbarkPressed();
    }

    public static void DailyRunLoadUnreadyPostfix()
    {
        DailyRunLoadGameScreen.Current?.OnUnreadyPressed();
    }
}
