using System;
using System.Linq;
using System.Reflection;
using System.Text;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Discovers every concrete <see cref="Announcement"/> subclass at startup and
/// registers its global settings under <c>announcements.{key}/</c>. Each
/// announcement gets an "enabled" BoolSetting by default, plus anything it
/// declares in an optional static <c>RegisterSettings(CategorySetting)</c> method
/// (mirrors the pattern EventRegistry uses for events).
///
/// The key and display name are derived from the class name — <c>HpAnnouncement</c>
/// becomes key <c>hp</c>, display name <c>HP</c>; <c>MonsterIntentsAnnouncement</c>
/// becomes <c>monster_intents</c> / <c>Monster Intents</c>; etc. This keeps
/// announcement definitions uniform without requiring per-class attributes or
/// static properties.
/// </summary>
public static class AnnouncementRegistry
{
    public static void RegisterDefaults()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(Announcement).IsAssignableFrom(t));

        foreach (var type in types)
        {
            try
            {
                Register(type);
            }
            catch (Exception e)
            {
                Log.Error($"[AccessibilityMod] Announcement registration failed for {type.Name}: {e.Message}");
            }
        }
    }

    private const string EnabledLocKey = "SETTINGS.ANNOUNCEMENT.ENABLED";
    private const string RootLocKey = "SETTINGS.ANNOUNCEMENTS_ROOT";

    private static void Register(Type announcementType)
    {
        var key = DeriveKey(announcementType);
        var displayName = DeriveDisplayName(announcementType);
        var categoryLocKey = $"SETTINGS.ANNOUNCEMENTS.{key.ToUpperInvariant()}";

        var category = ModSettingsRegistry.EnsureCategory(
            $"announcements.{key}",
            $"Announcements/{displayName}",
            $"{RootLocKey}/{categoryLocKey}");

        if (category.GetByKey("enabled") == null)
            category.Add(new BoolSetting("enabled", "Announce", true, localizationKey: EnabledLocKey));

        // Optional per-announcement extras (verbose toggles, etc.)
        var method = announcementType.GetMethod("RegisterSettings",
            BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(CategorySetting) }, null);
        method?.Invoke(null, new object[] { category });
    }

    /// <summary>Converts e.g. <c>MonsterIntentsAnnouncement</c> to <c>monster_intents</c>.</summary>
    public static string DeriveKey(Type announcementType)
    {
        var name = StripAnnouncementSuffix(announcementType.Name);
        var sb = new StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }

    /// <summary>Converts e.g. <c>MonsterIntentsAnnouncement</c> to <c>Monster Intents</c>.</summary>
    private static string DeriveDisplayName(Type announcementType)
    {
        var name = StripAnnouncementSuffix(announcementType.Name);
        var sb = new StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }

    private static string StripAnnouncementSuffix(string name)
    {
        const string suffix = "Announcement";
        return name.EndsWith(suffix) ? name[..^suffix.Length] : name;
    }
}
