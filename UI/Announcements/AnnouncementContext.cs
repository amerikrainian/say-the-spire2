using SayTheSpire2.Settings;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Supplied to <see cref="Announcement.Render"/> so announcements can resolve
/// per-announcement settings with the standard cascade: per-element override
/// if set, else global default. Built once per focus message by the composer.
/// </summary>
public sealed class AnnouncementContext
{
    /// <summary>The element currently being composed.</summary>
    public UIElement Element { get; }

    /// <summary>
    /// Cached element key derived from <see cref="UIElement.AnnouncementOrderType"/>.
    /// Used as the <c>ui.{key}</c> prefix for per-element override lookups.
    /// </summary>
    public string ElementKey { get; }

    public AnnouncementContext(UIElement element)
    {
        Element = element;
        ElementKey = AnnouncementRegistry.DeriveElementKey(element.AnnouncementOrderType);
    }

    public bool ResolveBool(string announcementKey, string settingKey, bool defaultValue)
    {
        var nullable = ModSettings.GetSetting<NullableBoolSetting>(
            $"ui.{ElementKey}.announcements.{announcementKey}.{settingKey}");
        if (nullable != null)
            return nullable.Resolved;

        var global = ModSettings.GetSetting<BoolSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }

    public int ResolveInt(string announcementKey, string settingKey, int defaultValue)
    {
        var nullable = ModSettings.GetSetting<NullableIntSetting>(
            $"ui.{ElementKey}.announcements.{announcementKey}.{settingKey}");
        if (nullable != null)
            return nullable.Resolved;

        var global = ModSettings.GetSetting<IntSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }

    public string ResolveString(string announcementKey, string settingKey, string defaultValue)
    {
        var nullable = ModSettings.GetSetting<NullableStringSetting>(
            $"ui.{ElementKey}.announcements.{announcementKey}.{settingKey}");
        if (nullable != null)
            return nullable.Resolved;

        var global = ModSettings.GetSetting<StringSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }

    public string ResolveChoice(string announcementKey, string settingKey, string defaultValue)
    {
        var nullable = ModSettings.GetSetting<NullableChoiceSetting>(
            $"ui.{ElementKey}.announcements.{announcementKey}.{settingKey}");
        if (nullable != null)
            return nullable.Resolved;

        var global = ModSettings.GetSetting<ChoiceSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }
}
