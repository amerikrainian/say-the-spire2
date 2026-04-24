using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Turns an element's yielded announcements into a final spoken focus message.
/// Reads the element's [AnnouncementOrder] attribute, sorts yielded announcements
/// by that order (undeclared ones appended at the end in yield order), renders
/// each, and joins them. Each announcement's Suffix sits between it and the next
/// announcement, so the last announcement's suffix is intentionally dropped —
/// no trailing punctuation.
/// </summary>
public static class AnnouncementComposer
{
    public static Message Compose(UIElement element, IEnumerable<Announcement> announcements)
    {
        var ctx = new AnnouncementContext(element);
        var attrOrder = element.AnnouncementOrderType.GetCustomAttribute<AnnouncementOrderAttribute>()?.Types
            ?? Array.Empty<Type>();
        var order = ResolveUserOrder(ctx.ElementKey, attrOrder);

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

        // Render, skip disabled (per-element override, else global), skip empty
        var rendered = new List<(string Text, string Suffix)>();
        foreach (var a in sorted)
        {
            if (!ctx.ResolveBool(a.Key, "enabled", true)) continue;
            var text = a.Render(ctx)?.Resolve();
            if (!string.IsNullOrEmpty(text))
                rendered.Add((text, a.Suffix));
        }

        if (rendered.Count == 0) return Message.Empty;

        // Join: each announcement's suffix sits between it and the next, space-separated.
        // The last announcement's suffix is dropped (no trailing punctuation).
        var sb = new StringBuilder();
        for (int i = 0; i < rendered.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(rendered[i - 1].Suffix);
                sb.Append(' ');
            }
            sb.Append(rendered[i].Text);
        }

        return Message.Raw(sb.ToString());
    }

    /// <summary>
    /// Applies the user-stored announcement order (e.g. from
    /// <c>ui.{element}.announcements.order</c>) on top of the type's attribute
    /// order. User-listed keys come first in their stored order; any types
    /// from the attribute not listed by the user are appended in attribute
    /// order. PositionAnnouncement is always included (auto-injected).
    /// </summary>
    private static Type[] ResolveUserOrder(string elementKey, Type[] attrOrder)
    {
        var orderSetting = ModSettings.GetSetting<StringSetting>($"ui.{elementKey}.announcements.order");
        if (orderSetting == null || string.IsNullOrWhiteSpace(orderSetting.Value))
            return attrOrder;

        // Key → Type map from attribute order plus PositionAnnouncement.
        var keyToType = new Dictionary<string, Type>();
        foreach (var t in attrOrder)
            keyToType[AnnouncementRegistry.DeriveAnnouncementKey(t)] = t;
        keyToType[AnnouncementRegistry.DeriveAnnouncementKey(typeof(PositionAnnouncement))] = typeof(PositionAnnouncement);

        var result = new List<Type>(keyToType.Count);
        var seen = new HashSet<Type>();
        foreach (var rawKey in orderSetting.Value.Split(','))
        {
            var key = rawKey.Trim();
            if (!string.IsNullOrEmpty(key) && keyToType.TryGetValue(key, out var t) && seen.Add(t))
                result.Add(t);
        }
        // Fill in anything the user's list didn't cover — e.g. a new announcement
        // type added by a mod update after the user last saved their order.
        foreach (var t in attrOrder)
            if (seen.Add(t)) result.Add(t);
        if (seen.Add(typeof(PositionAnnouncement)))
            result.Add(typeof(PositionAnnouncement));

        return result.ToArray();
    }
}
