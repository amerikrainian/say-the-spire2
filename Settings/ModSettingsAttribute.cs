using System;

namespace SayTheSpire2.Settings;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ModSettingsAttribute : Attribute
{
    public string Path { get; }
    public string Label { get; }
    /// <summary>
    /// Optional slash-separated localization key path matching <see cref="Path"/>.
    /// Each segment resolves the corresponding category's display label from
    /// <c>Localization/eng/ui.json</c>. Empty means no localization (raw label).
    /// </summary>
    public string LocalizationKey { get; }

    public ModSettingsAttribute(string path, string label, string localizationKey = "")
    {
        Path = path;
        Label = label;
        LocalizationKey = localizationKey;
    }
}
