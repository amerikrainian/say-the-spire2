using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Timeline;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class EpochInspectScreen : GameScreen
{
    public static EpochInspectScreen? Current { get; private set; }

    public override Message? ScreenName => null; // Announced via OnOpen instead

    protected override void BuildRegistry()
    {
    }

    public override void OnPush()
    {
        base.OnPush();
        Current = this;
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    public void OnOpen(EpochModel epoch, bool wasRevealed)
    {
        try
        {
            var parts = new List<Message>();

            AddEpochHeader(parts, epoch);

            if (wasRevealed)
                parts.Add(Message.Localized("ui", "TIMELINE.REVEALED"));

            var desc = epoch.Description;
            if (!string.IsNullOrEmpty(desc))
                parts.Add(Message.Raw(Message.StripBbcode(desc)));

            try
            {
                var unlockText = epoch.UnlockText;
                if (!string.IsNullOrEmpty(unlockText))
                    parts.Add(Message.Raw(Message.StripBbcode(unlockText)));
            }
            catch (Exception e) { Log.Error($"[AccessibilityMod] Epoch unlock text access failed: {e.Message}"); }

            if (parts.Count > 0)
            {
                var message = Message.Join(". ", parts.ToArray());
                Log.Info($"[AccessibilityMod] Epoch inspect: {message.Resolve()}");
                SpeechManager.Output(message);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Epoch inspect error: {ex.Message}");
        }
    }

    public void OnPaginate(EpochModel epoch)
    {
        try
        {
            var parts = new List<Message>();

            AddEpochHeader(parts, epoch);

            var desc = epoch.Description;
            if (!string.IsNullOrEmpty(desc))
                parts.Add(Message.Raw(Message.StripBbcode(desc)));

            try
            {
                var unlockText = epoch.UnlockText;
                if (!string.IsNullOrEmpty(unlockText))
                    parts.Add(Message.Raw(Message.StripBbcode(unlockText)));
            }
            catch (Exception e) { Log.Error($"[AccessibilityMod] Epoch paginate unlock text access failed: {e.Message}"); }

            if (parts.Count > 0)
            {
                var message = Message.Join(". ", parts.ToArray());
                Log.Info($"[AccessibilityMod] Epoch paginate: {message.Resolve()}");
                SpeechManager.Output(message);
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Epoch paginate error: {ex.Message}");
        }
    }

    private static void AddEpochHeader(List<Message> parts, EpochModel epoch)
    {
        var storyTitle = epoch.StoryTitle;
        var title = epoch.Title.GetFormattedText();
        if (!string.IsNullOrEmpty(storyTitle))
        {
            var chapterIndex = epoch.ChapterIndex;
            parts.Add(Message.Localized("ui", "EPOCH.CHAPTER", new { index = chapterIndex, title }));
            parts.Add(Message.Raw(storyTitle));
        }
        else if (!string.IsNullOrEmpty(title))
        {
            parts.Add(Message.Raw(title));
        }
    }
}
