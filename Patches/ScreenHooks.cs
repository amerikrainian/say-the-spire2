using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using MegaCrit.Sts2.Core.Timeline;
using SayTheSpire2.Buffers;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class ScreenHooks
{
    public static void Initialize(Harmony harmony)
    {
        var updateMethod = AccessTools.Method(typeof(ActiveScreenContext), "Update");
        if (updateMethod == null)
        {
            Log.Error("[AccessibilityMod] Could not find ActiveScreenContext.Update()!");
            return;
        }

        var postfix = new HarmonyMethod(typeof(ScreenHooks).GetMethod(nameof(UpdatePostfix), BindingFlags.Static | BindingFlags.Public));
        harmony.Patch(updateMethod, postfix: postfix);
        Log.Info("[AccessibilityMod] Screen hooks patched successfully.");

        // Hook game over screen to read banner title and death quote
        var initBanner = AccessTools.Method(typeof(NGameOverScreen), "InitializeBannerAndQuote");
        if (initBanner != null)
        {
            harmony.Patch(initBanner,
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(GameOverBannerPostfix)));
            Log.Info("[AccessibilityMod] GameOver banner hook patched.");
        }

        // Hook AddBadge to speak each badge and add to UI buffer
        var addBadge = AccessTools.Method(typeof(NGameOverScreen), "AddBadge");
        if (addBadge != null)
        {
            harmony.Patch(addBadge,
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(AddBadgePostfix)));
            Log.Info("[AccessibilityMod] GameOver AddBadge hook patched.");
        }

        // Hook epoch inspect screen Open to announce epoch details
        var epochOpen = AccessTools.Method(typeof(NEpochInspectScreen), "Open");
        if (epochOpen != null)
        {
            harmony.Patch(epochOpen,
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(EpochInspectOpenPostfix)));
            Log.Info("[AccessibilityMod] Epoch inspect Open hook patched.");
        }

        // Hook epoch inspect OpenViaPaginator to announce chapter navigation
        var epochPaginate = AccessTools.Method(typeof(NEpochInspectScreen), "OpenViaPaginator");
        if (epochPaginate != null)
        {
            harmony.Patch(epochPaginate,
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(EpochPaginatePostfix)));
            Log.Info("[AccessibilityMod] Epoch paginate hook patched.");
        }

        // Hook unlock screen Open to announce unlock banners
        var unlockOpen = AccessTools.Method(typeof(NUnlockScreen), "Open");
        if (unlockOpen != null)
        {
            harmony.Patch(unlockOpen,
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(UnlockScreenOpenPostfix)));
            Log.Info("[AccessibilityMod] Unlock screen Open hook patched.");
        }

        // Hook AnimateScoreBar to announce total score
        var animateScore = AccessTools.Method(typeof(NGameOverScreen), "AnimateScoreBar");
        if (animateScore != null)
        {
            harmony.Patch(animateScore,
                prefix: new HarmonyMethod(typeof(ScreenHooks), nameof(AnimateScoreBarPrefix)));
            Log.Info("[AccessibilityMod] GameOver AnimateScoreBar hook patched.");
        }
    }

    public static void UpdatePostfix()
    {
        ScreenManager.OnGameScreenChanged();
    }

    private static void AddEpochHeaderToParts(System.Collections.Generic.List<string> parts, EpochModel epoch)
    {
        var storyTitle = epoch.StoryTitle;
        var title = epoch.Title.GetFormattedText();
        if (!string.IsNullOrEmpty(storyTitle))
        {
            // Multi-chapter story: "Chapter X - Title. Story Name"
            var chapterIndex = epoch.ChapterIndex;
            parts.Add($"Chapter {chapterIndex} - {title}");
            parts.Add(storyTitle);
        }
        else if (!string.IsNullOrEmpty(title))
        {
            parts.Add(title);
        }
    }

    public static void EpochInspectOpenPostfix(EpochModel epoch, bool wasRevealed)
    {
        try
        {
            var parts = new System.Collections.Generic.List<string>();

            AddEpochHeaderToParts(parts, epoch);

            if (wasRevealed)
                parts.Add("revealed");

            // Description
            var desc = epoch.Description;
            if (!string.IsNullOrEmpty(desc))
                parts.Add(ProxyElement.StripBbcode(desc));

            // Unlock text (what was unlocked)
            try
            {
                var unlockText = epoch.UnlockText;
                if (!string.IsNullOrEmpty(unlockText))
                    parts.Add(ProxyElement.StripBbcode(unlockText));
            }
            catch { }

            if (parts.Count > 0)
            {
                var message = string.Join(". ", parts);
                Log.Info($"[AccessibilityMod] Epoch inspect: {message}");
                SpeechManager.Output(message, interrupt: true);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Epoch inspect hook error: {ex.Message}");
        }
    }

    public static void EpochPaginatePostfix(EpochModel epoch)
    {
        try
        {
            var parts = new System.Collections.Generic.List<string>();

            AddEpochHeaderToParts(parts, epoch);

            var desc = epoch.Description;
            if (!string.IsNullOrEmpty(desc))
                parts.Add(ProxyElement.StripBbcode(desc));

            try
            {
                var unlockText = epoch.UnlockText;
                if (!string.IsNullOrEmpty(unlockText))
                    parts.Add(ProxyElement.StripBbcode(unlockText));
            }
            catch { }

            if (parts.Count > 0)
            {
                var message = string.Join(". ", parts);
                Log.Info($"[AccessibilityMod] Epoch paginate: {message}");
                SpeechManager.Output(message, interrupt: true);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Epoch paginate hook error: {ex.Message}");
        }
    }

    private static readonly FieldInfo? ScoreField =
        typeof(NGameOverScreen).GetField("_score", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void AnimateScoreBarPrefix(NGameOverScreen __instance)
    {
        try
        {
            var score = ScoreField?.GetValue(__instance);
            if (score is int scoreVal)
            {
                var message = $"Score: {scoreVal}";
                Log.Info($"[AccessibilityMod] {message}");
                SpeechManager.Output(message);

                var uiBuffer = BufferManager.Instance.GetBuffer("ui");
                if (uiBuffer != null)
                {
                    uiBuffer.Add(message);
                    BufferManager.Instance.EnableBuffer("ui", true);
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] AnimateScoreBar hook error: {ex.Message}");
        }
    }

    public static void UnlockScreenOpenPostfix(NUnlockScreen __instance)
    {
        try
        {
            var parts = new System.Collections.Generic.List<string>();

            // Try %Banner (relics, cards, potions screens)
            var banner = __instance.GetNodeOrNull("%Banner");
            if (banner != null)
            {
                var bannerText = ProxyElement.FindChildTextPublic(banner);
                if (!string.IsNullOrEmpty(bannerText))
                    parts.Add(bannerText);
            }

            // Try %ExplanationText (relics, cards, potions screens)
            var explanation = __instance.GetNodeOrNull("%ExplanationText");
            if (explanation is RichTextLabel explRtl)
            {
                var text = ProxyElement.StripBbcode(explRtl.Text);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }

            // Try %Label (misc screen)
            var label = __instance.GetNodeOrNull("%Label");
            if (label is RichTextLabel labelRtl)
            {
                var text = ProxyElement.StripBbcode(labelRtl.Text);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }

            // Try %InfoLabel (epoch screen)
            var infoLabel = __instance.GetNodeOrNull("%InfoLabel");
            if (infoLabel is RichTextLabel infoRtl)
            {
                var text = ProxyElement.StripBbcode(infoRtl.Text);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }

            // Try %TopLabel and %BottomLabel (character unlock screen)
            var topLabel = __instance.GetNodeOrNull("%TopLabel");
            if (topLabel is RichTextLabel topRtl)
            {
                var text = ProxyElement.StripBbcode(topRtl.Text);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }

            var bottomLabel = __instance.GetNodeOrNull("%BottomLabel");
            if (bottomLabel is RichTextLabel bottomRtl)
            {
                var text = ProxyElement.StripBbcode(bottomRtl.Text);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }

            // Read unlocked item names via reflection
            var itemNames = GetUnlockedItemNames(__instance);
            if (itemNames != null)
                parts.Add(itemNames);

            if (parts.Count > 0)
            {
                var message = string.Join(". ", parts);
                Log.Info($"[AccessibilityMod] Unlock screen: {message}");
                SpeechManager.Output(message, interrupt: true);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Unlock screen hook error: {ex.Message}");
        }
    }

    private static string? GetUnlockedItemNames(NUnlockScreen instance)
    {
        try
        {
            var type = instance.GetType();
            var names = new System.Collections.Generic.List<string>();

            // Try _cards field (NUnlockCardsScreen)
            var cardsField = type.GetField("_cards", BindingFlags.Instance | BindingFlags.NonPublic);
            if (cardsField?.GetValue(instance) is System.Collections.IEnumerable cards)
            {
                foreach (var card in cards)
                {
                    var title = card?.GetType().GetProperty("Title")?.GetValue(card)?.ToString();
                    if (!string.IsNullOrEmpty(title))
                        names.Add(title);
                }
            }

            // Try _relics field (NUnlockRelicsScreen)
            var relicsField = type.GetField("_relics", BindingFlags.Instance | BindingFlags.NonPublic);
            if (relicsField?.GetValue(instance) is System.Collections.IEnumerable relics)
            {
                foreach (var relic in relics)
                {
                    var title = relic?.GetType().GetProperty("Title")?.GetValue(relic);
                    var text = title?.GetType().GetMethod("GetFormattedText")?.Invoke(title, null)?.ToString();
                    if (!string.IsNullOrEmpty(text))
                        names.Add(text);
                }
            }

            // Try _potions field (NUnlockPotionsScreen)
            var potionsField = type.GetField("_potions", BindingFlags.Instance | BindingFlags.NonPublic);
            if (potionsField?.GetValue(instance) is System.Collections.IEnumerable potions)
            {
                foreach (var potion in potions)
                {
                    var title = potion?.GetType().GetProperty("Title")?.GetValue(potion);
                    var text = title?.GetType().GetMethod("GetFormattedText")?.Invoke(title, null)?.ToString();
                    if (!string.IsNullOrEmpty(text))
                        names.Add(text);
                }
            }

            // Try _unlockedEpochs field (NUnlockEpochScreen)
            var epochsField = type.GetField("_unlockedEpochs", BindingFlags.Instance | BindingFlags.NonPublic);
            if (epochsField?.GetValue(instance) is System.Collections.IEnumerable epochs)
            {
                foreach (var epoch in epochs)
                {
                    var title = epoch?.GetType().GetProperty("Title")?.GetValue(epoch);
                    var text = title?.GetType().GetMethod("GetFormattedText")?.Invoke(title, null)?.ToString();
                    if (!string.IsNullOrEmpty(text))
                        names.Add(text);
                }
            }

            return names.Count > 0 ? string.Join(", ", names) : null;
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Failed to read unlock items: {ex.Message}");
            return null;
        }
    }

    public static void AddBadgePostfix(string locEntryKey, string? locAmountKey, int amount)
    {
        try
        {
            var locString = new LocString("game_over_screen", locEntryKey);
            if (locAmountKey != null)
                locString.Add(locAmountKey, amount);
            var text = locString.GetFormattedText();
            if (string.IsNullOrEmpty(text)) return;

            var stripped = ProxyElement.StripBbcode(text);
            Log.Info($"[AccessibilityMod] Badge: {stripped}");
            SpeechManager.Output(stripped);

            var uiBuffer = BufferManager.Instance.GetBuffer("ui");
            if (uiBuffer != null)
            {
                uiBuffer.Add(stripped);
                BufferManager.Instance.EnableBuffer("ui", true);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] AddBadge hook error: {ex.Message}");
        }
    }

    public static void GameOverBannerPostfix(NGameOverScreen __instance)
    {
        try
        {
            var banner = __instance.GetNodeOrNull("%Banner");
            var quoteLabel = __instance.GetNodeOrNull("%DeathQuoteLabel");

            string? title = null;
            if (banner != null)
                title = ProxyElement.FindChildTextPublic(banner);

            string? quote = null;
            if (quoteLabel is RichTextLabel rtl)
                quote = ProxyElement.StripBbcode(rtl.Text);

            var message = "";
            if (!string.IsNullOrEmpty(title))
                message = title;
            if (!string.IsNullOrEmpty(quote))
                message += string.IsNullOrEmpty(message) ? quote : $". {quote}";

            if (!string.IsNullOrEmpty(message))
            {
                Log.Info($"[AccessibilityMod] Game over: {message}");
                SpeechManager.Output(message, interrupt: true);

                // Clear UI buffer for incoming badges
                var uiBuffer = BufferManager.Instance.GetBuffer("ui");
                if (uiBuffer != null)
                {
                    uiBuffer.Clear();
                    uiBuffer.Add(message);
                    BufferManager.Instance.EnableBuffer("ui", true);
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] GameOver banner hook error: {ex.Message}");
        }
    }
}
