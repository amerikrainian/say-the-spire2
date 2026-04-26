using SayTheSpire2.Settings;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Supplied to <see cref="Announcement.Render"/> so announcements can resolve
/// per-announcement settings with a two-level override cascade:
/// inner element → outer proxy → global default (most specific wins).
///
/// <para>The inner key is derived from <see cref="UIElement.AnnouncementOrderType"/>
/// which also governs ordering — for a merchant slot wrapping a potion, the
/// inner key is <c>potion</c>. The outer key is the focused element's actual
/// type (e.g. <c>ProxyMerchantSlot</c> → <c>merchant_slot</c>). When the two
/// match — a non-composite proxy, or a composite whose inner is null — only
/// one lookup happens.</para>
///
/// <para>Inner takes priority because it's the more specific context: if the
/// user explicitly disables price on cards, a merchant_slot "enable prices"
/// override shouldn't silently re-enable it. The outer still acts as the
/// natural "disable price on all shop items" knob as long as inner stays
/// on inherit.</para>
/// </summary>
public sealed class AnnouncementContext
{
    /// <summary>The element currently being composed.</summary>
    public UIElement Element { get; }

    /// <summary>
    /// Element key derived from <see cref="UIElement.AnnouncementOrderType"/>.
    /// Drives ordering and the second-priority override lookup.
    /// </summary>
    public string ElementKey { get; }

    /// <summary>
    /// Element key derived from the focused element's actual type. Null when
    /// it matches <see cref="ElementKey"/> (non-composite proxies). Second
    /// priority in the override cascade — inner wins if explicitly set.
    /// </summary>
    public string? OuterKey { get; }

    public AnnouncementContext(UIElement element)
    {
        Element = element;
        var innerType = element.AnnouncementOrderType;
        ElementKey = AnnouncementRegistry.DeriveElementKey(innerType);

        var outerType = element.GetType();
        if (outerType != innerType)
        {
            var outerKey = AnnouncementRegistry.DeriveElementKey(outerType);
            if (outerKey != ElementKey)
                OuterKey = outerKey;
        }
    }

    public bool ResolveBool(string announcementKey, string settingKey, bool defaultValue)
    {
        var inner = ModSettings.GetSetting<NullableBoolSetting>(
            $"ui.{ElementKey}.announcements.{announcementKey}.{settingKey}");
        if (inner?.IsOverridden == true)
            return inner.LocalValue!.Value;

        if (OuterKey != null)
        {
            var outer = ModSettings.GetSetting<NullableBoolSetting>(
                $"ui.{OuterKey}.announcements.{announcementKey}.{settingKey}");
            if (outer?.IsOverridden == true)
                return outer.LocalValue!.Value;
        }

        var global = ModSettings.GetSetting<BoolSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }

    public int ResolveInt(string announcementKey, string settingKey, int defaultValue)
    {
        var inner = ModSettings.GetSetting<NullableIntSetting>(
            $"ui.{ElementKey}.announcements.{announcementKey}.{settingKey}");
        if (inner?.IsOverridden == true)
            return inner.LocalValue!.Value;

        if (OuterKey != null)
        {
            var outer = ModSettings.GetSetting<NullableIntSetting>(
                $"ui.{OuterKey}.announcements.{announcementKey}.{settingKey}");
            if (outer?.IsOverridden == true)
                return outer.LocalValue!.Value;
        }

        var global = ModSettings.GetSetting<IntSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }

    public string ResolveString(string announcementKey, string settingKey, string defaultValue)
    {
        var inner = ModSettings.GetSetting<NullableStringSetting>(
            $"ui.{ElementKey}.announcements.{announcementKey}.{settingKey}");
        if (inner?.IsOverridden == true)
            return inner.LocalValue!;

        if (OuterKey != null)
        {
            var outer = ModSettings.GetSetting<NullableStringSetting>(
                $"ui.{OuterKey}.announcements.{announcementKey}.{settingKey}");
            if (outer?.IsOverridden == true)
                return outer.LocalValue!;
        }

        var global = ModSettings.GetSetting<StringSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }

    public string ResolveChoice(string announcementKey, string settingKey, string defaultValue)
    {
        var inner = ModSettings.GetSetting<NullableChoiceSetting>(
            $"ui.{ElementKey}.announcements.{announcementKey}.{settingKey}");
        if (inner?.IsOverridden == true)
            return inner.LocalValue!;

        if (OuterKey != null)
        {
            var outer = ModSettings.GetSetting<NullableChoiceSetting>(
                $"ui.{OuterKey}.announcements.{announcementKey}.{settingKey}");
            if (outer?.IsOverridden == true)
                return outer.LocalValue!;
        }

        var global = ModSettings.GetSetting<ChoiceSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }
}
