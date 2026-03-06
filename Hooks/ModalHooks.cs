using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Sts2AccessibilityMod.Speech;
using Sts2AccessibilityMod.UI;

namespace Sts2AccessibilityMod.Hooks;

public static class ModalHooks
{
    public static void Initialize(Harmony harmony)
    {
        var addMethod = AccessTools.Method(typeof(NModalContainer), "Add");
        if (addMethod != null)
        {
            harmony.Patch(addMethod,
                postfix: new HarmonyMethod(typeof(ModalHooks), nameof(AddPostfix)));
            Log.Info("[AccessibilityMod] NModalContainer.Add hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NModalContainer.Add()!");
        }

    }

    public static void AddPostfix(Node modalToCreate)
    {
        // Wait one frame for _Ready to run and populate text nodes
        var tree = modalToCreate.GetTree();
        if (tree != null)
        {
            // Use a local variable to hold the handler so we can disconnect after one call
            SignalAwaiter awaiter = modalToCreate.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            awaiter.OnCompleted(() => AnnounceModal(modalToCreate));
        }
        else
        {
            AnnounceModal(modalToCreate);
        }
    }

    private static void AnnounceModal(Node modal)
    {
        if (!GodotObject.IsInstanceValid(modal)) return;

        var sb = new StringBuilder();

        // Look for header/title text
        var header = FindTextByNames(modal, new[] { "Header", "Title", "TitleLabel" });
        if (!string.IsNullOrEmpty(header))
            sb.Append(header);

        // Look for body/description text
        var body = FindTextByNames(modal, new[] { "Description", "Body", "BodyLabel" });
        if (!string.IsNullOrEmpty(body))
        {
            if (sb.Length > 0) sb.Append(". ");
            sb.Append(body);
        }

        if (sb.Length > 0)
        {
            var text = sb.ToString();
            Log.Info($"[AccessibilityMod] Modal opened: \"{text}\"");
            SpeechManager.Output(text);
        }
        else
        {
            Log.Info($"[AccessibilityMod] Modal opened: {modal.GetType().Name} (no readable text found)");
        }
    }

    private static string? FindTextByNames(Node root, string[] names)
    {
        foreach (var name in names)
        {
            var text = FindTextNodeRecursive(root, name);
            if (text != null) return text;
        }
        return null;
    }

    private static string? FindTextNodeRecursive(Node node, string targetName)
    {
        // Check if this node matches the target name
        if (node.Name == targetName)
        {
            return ExtractText(node);
        }

        // Check children
        for (int i = 0; i < node.GetChildCount(); i++)
        {
            var result = FindTextNodeRecursive(node.GetChild(i), targetName);
            if (result != null) return result;
        }

        return null;
    }

    private static string? ExtractText(Node node)
    {
        if (node is RichTextLabel rtl && !string.IsNullOrWhiteSpace(rtl.Text))
            return ProxyElement.StripBbcode(rtl.Text);
        if (node is Label label && !string.IsNullOrWhiteSpace(label.Text))
            return label.Text;
        return null;
    }
}
