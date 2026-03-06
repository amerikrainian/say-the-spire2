using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

namespace SayTheSpire2.UI.Elements;

public abstract class ProxyElement : UIElement
{
    private static readonly Regex ImgPattern = new(@"\[img\](.*?)\[/img\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BbcodePattern = new(@"\[.*?\]", RegexOptions.Compiled);
    private static readonly Regex CamelCasePattern = new(@"([a-z])([A-Z])", RegexOptions.Compiled);
    private static readonly Regex ResPathPattern = new(@"res://\S+", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> IconNames = new()
    {
        { "energy_icon", "Energy" },
        { "star_icon", "Star" },
        { "gold_icon", "Gold" },
        { "card_icon", "Card" },
        { "chest_icon", "Chest" },
    };

    protected Control Control { get; private set; }

    public string? OverrideLabel { get; set; }

    protected ProxyElement(Control control)
    {
        Control = control;
    }

    public void SetControl(Control control)
    {
        Control = control;
    }

    public static string? FindChildTextPublic(Node node) => FindChildText(node);

    protected static string? FindChildText(Node node)
    {
        if (node is Label label && !string.IsNullOrWhiteSpace(label.Text))
            return label.Text;
        if (node is RichTextLabel rtl && !string.IsNullOrWhiteSpace(rtl.Text))
            return StripBbcode(rtl.Text);

        // Check well-known child names first
        foreach (var childName in new[] { "Title", "Label", "%Label", "%Title" })
        {
            var child = node.GetNodeOrNull(childName);
            if (child != null)
            {
                var text = FindChildText(child);
                if (text != null) return text;
            }
        }

        // Walk all children
        for (int i = 0; i < node.GetChildCount(); i++)
        {
            var child = node.GetChild(i);
            var text = FindChildText(child);
            if (text != null) return text;
        }

        return null;
    }

    protected static string? FindSiblingLabel(Node node)
    {
        var parent = node.GetParent();
        if (parent == null) return null;

        // Look for a Label sibling in the parent container
        foreach (var childName in new[] { "Label", "%Label", "Title", "%Title" })
        {
            var sibling = parent.GetNodeOrNull(childName);
            if (sibling != null && sibling != node)
            {
                var text = FindChildText(sibling);
                if (text != null) return text;
            }
        }

        return null;
    }

    public static string StripBbcode(string text)
    {
        // Replace [img]res://path/icon_name.png[/img] with readable names
        text = ImgPattern.Replace(text, m => ResolveIconPath(m.Groups[1].Value));
        // Strip remaining BBCode tags
        text = BbcodePattern.Replace(text, "");
        // Catch any stray res:// paths not wrapped in [img] tags
        text = ResPathPattern.Replace(text, m => ResolveIconPath(m.Value));
        return text.Trim();
    }

    private static string ResolveIconPath(string path)
    {
        // Extract filename without extension: "res://images/.../ironclad_energy_icon.png" -> "ironclad_energy_icon"
        var lastSlash = path.LastIndexOf('/');
        var name = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
        var dot = name.LastIndexOf('.');
        if (dot > 0) name = name.Substring(0, dot);

        // Check known icon suffixes (e.g. "ironclad_energy_icon" matches "energy_icon")
        foreach (var (suffix, label) in IconNames)
        {
            if (name.EndsWith(suffix) || name == suffix)
                return label;
        }

        // Fallback: clean up the filename ("some_icon_name" -> "Some Icon Name")
        name = name.Replace("_", " ").Trim();
        return name;
    }

    protected static string CleanNodeName(string name)
    {
        return CamelCasePattern.Replace(name, "$1 $2");
    }
}
