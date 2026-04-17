using SayTheSpire2.Localization;

namespace SayTheSpire2.Settings;

public abstract class Setting
{
    public string Key { get; }
    public CategorySetting? Parent { get; internal set; }

    private readonly string _labelFallback;

    /// <summary>
    /// Optional localization key. When set, <see cref="Label"/> resolves from
    /// <c>Localization/eng/ui.json</c> at read time, falling back to the raw
    /// label string if the key is missing. Empty string means "no localization,
    /// use raw label" (the pre-localization behavior).
    /// </summary>
    public string LocalizationKey { get; }

    /// <summary>
    /// Display label. Resolves the localization key if one was supplied at
    /// construction, otherwise returns the raw label passed in.
    /// </summary>
    public string Label => !string.IsNullOrEmpty(LocalizationKey)
        ? LocalizationManager.GetOrDefault("ui", LocalizationKey, _labelFallback)
        : _labelFallback;

    /// <summary>
    /// Whether this setting's key contributes to its serialized dot-path.
    /// UI-only grouping categories can opt out so reorganizing the menu
    /// does not break saved settings paths.
    /// </summary>
    public virtual bool IncludeInPath => true;

    /// <summary>
    /// Lower values sort first. Settings are sorted by priority, then alphabetically within each level.
    /// Default is 0.
    /// </summary>
    public int SortPriority { get; set; }

    protected Setting(string key, string label, string localizationKey = "")
    {
        Key = key;
        _labelFallback = label;
        LocalizationKey = localizationKey;
    }

    public string FullPath
    {
        get
        {
            var parentPath = Parent == null || Parent.IsRoot ? string.Empty : Parent.FullPath;
            if (!IncludeInPath)
                return parentPath;
            if (string.IsNullOrEmpty(parentPath))
                return Key;
            return $"{parentPath}.{Key}";
        }
    }

    /// <summary>
    /// Whether this is a root category (no parent, no key in path).
    /// </summary>
    public virtual bool IsRoot => false;

    /// <summary>
    /// Get the current value as object for serialization.
    /// Returns null for categories.
    /// </summary>
    public abstract object? BoxedValue { get; }

    /// <summary>
    /// Load a value from deserialized JSON.
    /// </summary>
    public abstract void LoadValue(object? value);
}
