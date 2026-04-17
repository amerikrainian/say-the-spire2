using System;
using System.Reflection;

namespace SayTheSpire2.Settings;

public static class ModSettingsRegistry
{
    /// <summary>
    /// Ensures all categories in the dot-separated path exist, creating missing ones.
    /// Label segments are slash-separated and used as fallback English display names.
    /// Optionally accepts a slash-separated localizationKey string so each created
    /// category resolves its label from ui.json. Returns the leaf category.
    /// </summary>
    public static CategorySetting EnsureCategory(string path, string label, string localizationKey = "")
    {
        var pathParts = path.Split('.');
        var labelParts = label.Split('/');
        var locParts = string.IsNullOrEmpty(localizationKey)
            ? System.Array.Empty<string>()
            : localizationKey.Split('/');

        CategorySetting current = ModSettings.Root;
        for (int i = 0; i < pathParts.Length; i++)
        {
            var key = pathParts[i];
            var existing = current.GetByKey(key) as CategorySetting;
            if (existing != null)
            {
                current = existing;
            }
            else
            {
                var segmentLabel = i < labelParts.Length ? labelParts[i].Trim() : key;
                var segmentLocKey = i < locParts.Length ? locParts[i].Trim() : string.Empty;
                var newCat = new CategorySetting(key, segmentLabel, localizationKey: segmentLocKey);
                current.Add(newCat);
                current = newCat;
            }
        }
        return current;
    }

    /// <summary>
    /// Reads the ModSettingsAttribute (or subclass) from the type, ensures
    /// the category hierarchy exists, and calls the optional static
    /// RegisterSettings(CategorySetting) method on the type.
    /// Returns the leaf category.
    /// </summary>
    public static CategorySetting Register(Type type)
    {
        var attr = (ModSettingsAttribute?)Attribute.GetCustomAttribute(
            type, typeof(ModSettingsAttribute));
        if (attr == null)
            throw new InvalidOperationException($"{type.Name} is missing [ModSettings] attribute");

        var cat = EnsureCategory(attr.Path, attr.Label);

        var method = type.GetMethod("RegisterSettings",
            BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(CategorySetting) }, null);
        method?.Invoke(null, new object[] { cat });

        return cat;
    }
}
