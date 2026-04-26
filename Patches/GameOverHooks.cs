using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class GameOverHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NGameOverScreen), "InitializeBannerAndQuote",
            typeof(GameOverHooks), nameof(GameOverBannerPostfix), "GameOver banner");
        // Beta 2026-04-23: NGameOverScreen.AddBadge is gone; badges are now
        // built via NBadge.Create(Badge). Patch the new factory and read the
        // title off the resulting NBadge instance.
        HarmonyHelper.PatchIfFound(harmony, typeof(NBadge), "Create",
            typeof(GameOverHooks), nameof(CreateBadgePostfix), "GameOver badge", parameterTypes: [typeof(Badge)]);
        HarmonyHelper.PatchIfFound(harmony, typeof(NGameOverScreen), "AnimateScoreBar",
            typeof(GameOverHooks), nameof(AnimateScoreBarPrefix), "GameOver AnimateScoreBar", isPrefix: true);
    }

    public static void GameOverBannerPostfix(NGameOverScreen __instance)
    {
        if (GameOverScreen.Current == null)
            ScreenManager.PushScreen(new GameOverScreen());
        GameOverScreen.Current?.OnBannerAndQuote(__instance);
    }

    public static void CreateBadgePostfix(NBadge? __result)
        => GameOverScreen.Current?.OnBadge(__result);

    public static void AnimateScoreBarPrefix(NGameOverScreen __instance)
        => GameOverScreen.Current?.OnScore(__instance);
}
