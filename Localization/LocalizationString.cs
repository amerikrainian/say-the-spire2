using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sts2AccessibilityMod.Localization;

public class LocalizationString
{
    private static readonly Regex VariablePattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

    private readonly string _table;
    private readonly string _key;
    private Dictionary<string, string>? _variables;

    public LocalizationString(string table, string key)
    {
        _table = table;
        _key = key;
    }

    public LocalizationString Add(string name, object value)
    {
        _variables ??= new Dictionary<string, string>();
        _variables[name] = value.ToString() ?? "";
        return this;
    }

    public override string ToString()
    {
        var template = LocalizationManager.Get(_table, _key);
        if (template == null)
            return $"[{_table}.{_key}]";

        if (_variables == null || _variables.Count == 0)
            return template;

        return VariablePattern.Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            return _variables.TryGetValue(name, out var value) ? value : match.Value;
        });
    }
}
