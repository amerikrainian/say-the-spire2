using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Events;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.Patches;

public static class EventHooks
{
    private static readonly FieldInfo? TitleField =
        typeof(NEventLayout).GetField("_title", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? DialogueContainerField =
        typeof(NAncientEventLayout).GetField("_dialogueContainer", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Initialize(Harmony harmony)
    {
        var setDescription = AccessTools.Method(typeof(NEventLayout), "SetDescription");
        if (setDescription != null)
        {
            harmony.Patch(setDescription,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(SetDescriptionPostfix)));
            Log.Info("[AccessibilityMod] Event SetDescription hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NEventLayout.SetDescription!");
        }

        var setDialogueLine = AccessTools.Method(typeof(NAncientEventLayout), "SetDialogueLineAndAnimate");
        if (setDialogueLine != null)
        {
            harmony.Patch(setDialogueLine,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(SetDialogueLinePostfix)));
            Log.Info("[AccessibilityMod] Ancient dialogue line hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NAncientEventLayout.SetDialogueLineAndAnimate!");
        }
    }

    public static void SetDialogueLinePostfix(NAncientEventLayout __instance, int lineIndex)
    {
        try
        {
            var container = DialogueContainerField?.GetValue(__instance) as Node;
            if (container == null) return;

            var child = container.GetChildOrNull<Control>(lineIndex);
            if (child == null) return;

            // NAncientDialogueLine has a %Text (MegaRichTextLabel extends RichTextLabel)
            var textNode = child.GetNodeOrNull<RichTextLabel>("%Text");
            if (textNode == null) return;

            var text = textNode.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var clean = ProxyElement.StripBbcode(text);
                if (!string.IsNullOrEmpty(clean))
                {
                    Log.Info($"[AccessibilityMod] Ancient dialogue: \"{clean}\"");
                    SpeechManager.Output(clean);
                }
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Ancient dialogue hook error: {e.Message}");
        }
    }

    public static void SetDescriptionPostfix(NEventLayout __instance, string description)
    {
        try
        {
            var title = "";
            var titleLabel = TitleField?.GetValue(__instance);
            if (titleLabel != null)
            {
                var textProp = titleLabel.GetType().GetProperty("Text");
                if (textProp != null)
                    title = textProp.GetValue(titleLabel) as string ?? "";
            }

            var cleanDesc = ProxyElement.StripBbcode(description);
            if (string.IsNullOrEmpty(cleanDesc)) return;

            var text = string.IsNullOrEmpty(title) ? cleanDesc : $"{title}. {cleanDesc}";
            Log.Info($"[AccessibilityMod] Event description: \"{text}\"");
            SpeechManager.Output(text);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Event description hook error: {e.Message}");
        }
    }
}
