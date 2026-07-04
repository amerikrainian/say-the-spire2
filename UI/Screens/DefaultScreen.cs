using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Screens.FeedbackScreen;
using SayTheSpire2.Buffers;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Screens;

public class DefaultScreen : Screen
{
    public DefaultScreen()
    {
        ClaimAction("buffer_next_item");
        ClaimAction("buffer_prev_item");
        ClaimAction("buffer_next");
        ClaimAction("buffer_prev");
        ClaimAction("reset_bindings");
        ClaimAction("force_english");
        ClaimAction("read_draw_pile");
        ClaimAction("read_discard_pile");
        ClaimAction("read_deck");
        ClaimAction("read_exhaust_pile");
        ClaimAction("read_hand");
        ClaimAction("mod_settings");
        ClaimAction("help");
        ClaimAction("dev_console");
        ClaimAction("feedback");
        ClaimAction("nav_home");
        ClaimAction("nav_end");
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            case "buffer_next_item":
                BufferControls.NextItem();
                return true;
            case "buffer_prev_item":
                BufferControls.PreviousItem();
                return true;
            case "buffer_next":
                BufferControls.NextBuffer();
                return true;
            case "buffer_prev":
                BufferControls.PreviousBuffer();
                return true;
            case "reset_bindings":
                Log.Info("[AccessibilityMod] Global hotkey: Ctrl+Shift+R - resetting mod bindings");
                InputManager.ResetToDefaults();
                Speech.SpeechManager.Output(Localization.Message.Localized("ui", "SPEECH.BINDINGS_RESET"));
                return true;
            case "force_english":
                ForceEnglish();
                return true;
            case "read_draw_pile":
                return AnnouncePile(PileReadout.Draw);
            case "read_discard_pile":
                return AnnouncePile(PileReadout.Discard);
            case "read_deck":
                return AnnouncePile(PileReadout.Deck);
            case "read_exhaust_pile":
                return AnnouncePile(PileReadout.Exhaust);
            case "read_hand":
                return AnnouncePile(PileReadout.Hand);
            case "mod_settings":
                OpenModMenu();
                return true;
            case "help":
                OpenHelpScreen();
                return true;
            case "dev_console":
                ToggleDevConsole();
                return true;
            case "feedback":
                OpenFeedbackScreen();
                return true;
            case "nav_home":
                ContainerNavigation.JumpToFirst();
                return true;
            case "nav_end":
                ContainerNavigation.JumpToLast();
                return true;
        }

        return false;
    }

    private static void OpenModMenu()
    {
        var screen = new ModMenuScreen();
        ScreenManager.PushScreen(screen);
    }

    /// <summary>
    /// Force the game (and therefore the mod) into English, for users whose
    /// game is stuck in a language they don't read. Mirrors what the language
    /// dropdown does — persist the choice and switch live. Our LocManager
    /// .SetLanguage hook propagates the change to the mod's own strings, so the
    /// confirmation below resolves in English.
    /// </summary>
    private static void ForceEnglish()
    {
        try
        {
            var save = MegaCrit.Sts2.Core.Saves.SaveManager.Instance;
            if (save?.SettingsSave != null)
                save.SettingsSave.Language = "eng";
            MegaCrit.Sts2.Core.Localization.LocManager.Instance?.SetLanguage("eng");
            save?.SaveSettings();
            Speech.SpeechManager.Output(Localization.Message.Localized("ui", "SPEECH.LANGUAGE_SET_ENGLISH"));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Force English failed: {e.Message}");
        }
    }

    private enum PileReadout { Draw, Discard, Deck, Exhaust, Hand }

    /// <summary>
    /// Speaks a pile's card count and contents ("Draw Pile: 5 cards, Strike,
    /// Strike, Bash"). Deck works anywhere in a run; the combat piles say
    /// "not in combat" outside one. Draw pile and deck names are sorted
    /// alphabetically so the readout doesn't leak the hidden pile order —
    /// the game's own pile view hides it the same way.
    /// </summary>
    private static bool AnnouncePile(PileReadout kind)
    {
        try
        {
            var state = MegaCrit.Sts2.Core.Runs.RunManager.Instance?.DebugOnlyGetState();
            var me = state != null ? MegaCrit.Sts2.Core.Context.LocalContext.GetMe(state) : null;
            if (me == null) return false;

            var pcs = me.PlayerCombatState;
            if (kind != PileReadout.Deck && pcs == null)
            {
                Speech.SpeechManager.Output(Localization.Message.Localized("ui", "SPEECH.NOT_IN_COMBAT"));
                return true;
            }

            var cards = kind switch
            {
                PileReadout.Draw => pcs!.DrawPile.Cards,
                PileReadout.Discard => pcs!.DiscardPile.Cards,
                PileReadout.Deck => me.Deck?.Cards,
                PileReadout.Exhaust => pcs!.ExhaustPile.Cards,
                PileReadout.Hand => pcs!.Hand.Cards,
                _ => null,
            };
            if (cards == null) return false;

            var titles = new System.Collections.Generic.List<string>();
            foreach (var card in cards)
                titles.Add(Views.CardView.FromModel(card).Title);
            if (kind is PileReadout.Draw or PileReadout.Deck)
                titles.Sort(StringComparer.CurrentCultureIgnoreCase);

            // No pile name prefix — the key pressed already says which pile
            // it is, and the goal is the quickest possible readout.
            var message = Localization.Message.Localized("ui", "SPEECH.PILE_COUNT",
                new { count = titles.Count });
            if (titles.Count > 0)
                message = Localization.Message.Join(", ", message,
                    Localization.Message.Raw(string.Join(", ", titles)));
            Speech.SpeechManager.Output(message);
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Pile readout failed: {e}");
            return false;
        }
    }

    private static void OpenHelpScreen()
    {
        var builder = new HelpScreenBuilder();
        builder.AddFromScreenStack();
        builder.AddAlwaysPresent();
        var screen = new HelpScreen(builder.Build());
        ScreenManager.PushScreen(screen);
    }

    private static void ToggleDevConsole()
    {
        try
        {
            var console = NDevConsole.Instance;
            if (console.Visible)
                console.HideConsole();
            else
                console.ShowConsole();
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Dev console toggle failed: {e.Message}");
        }
    }

    private static void OpenFeedbackScreen()
    {
        try
        {
            var opener = NFeedbackScreenOpener.Instance;
            if (opener == null) return;

            var feedbackScreen = MegaCrit.Sts2.Core.Nodes.NGame.Instance?.FeedbackScreen;
            if (feedbackScreen == null || feedbackScreen.Visible) return;

            MegaCrit.Sts2.Core.Helpers.TaskHelper.RunSafely(opener.OpenFeedbackScreen());
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Feedback screen failed: {e.Message}");
        }
    }
}
