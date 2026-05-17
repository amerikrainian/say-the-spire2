using SayTheSpire2.Localization;

namespace SayTheSpire2.Settings;

public class Choice
{
    private readonly string _labelFallback;

    public string Key { get; }
    public string LocalizationKey { get; }
    public object? Metadata { get; }

    /// <summary>
    /// Resolves the localization key if one was supplied at construction,
    /// otherwise returns the raw label. Dynamic so language switches take
    /// effect without rebuilding the choice list.
    /// </summary>
    public string Label => !string.IsNullOrEmpty(LocalizationKey)
        ? LocalizationManager.GetOrDefault("ui", LocalizationKey, _labelFallback)
        : _labelFallback;

    public Choice(string key, string label, object? metadata = null, string localizationKey = "")
    {
        Key = key;
        _labelFallback = label;
        Metadata = metadata;
        LocalizationKey = localizationKey;
    }
}
