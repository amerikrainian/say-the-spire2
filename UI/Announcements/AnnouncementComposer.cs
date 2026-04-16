using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Turns an element's yielded announcements into a final spoken focus message.
/// Reads the element's [AnnouncementOrder] attribute, sorts yielded announcements
/// by that order (undeclared ones appended at the end in yield order), renders
/// each, appends its suffix, and space-joins the results.
/// </summary>
public static class AnnouncementComposer
{
    public static Message Compose(UIElement element, IEnumerable<Announcement> announcements)
    {
        var order = element.GetType().GetCustomAttribute<AnnouncementOrderAttribute>()?.Types
            ?? Array.Empty<Type>();

        // Partition into declared (keyed by type) and undeclared (kept in yield order)
        var declared = new Dictionary<Type, Announcement>();
        var undeclared = new List<Announcement>();
        foreach (var a in announcements)
        {
            var t = a.GetType();
            if (order.Contains(t) && !declared.ContainsKey(t))
                declared[t] = a;
            else
                undeclared.Add(a);
        }

        // Emit declared in attribute order, then undeclared in yield order
        var sorted = new List<Announcement>(declared.Count + undeclared.Count);
        foreach (var t in order)
        {
            if (declared.TryGetValue(t, out var a))
                sorted.Add(a);
        }
        sorted.AddRange(undeclared);

        var parts = new List<string>();
        foreach (var a in sorted)
        {
            var rendered = a.Render()?.Resolve();
            if (string.IsNullOrEmpty(rendered)) continue;
            parts.Add(rendered + a.Suffix);
        }

        return parts.Count > 0 ? Message.Raw(string.Join(" ", parts)) : Message.Empty;
    }
}
