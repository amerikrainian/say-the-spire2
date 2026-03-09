using System;

namespace SayTheSpire2.Settings;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ModSettingsAttribute : Attribute
{
    public string Path { get; }
    public string Label { get; }

    public ModSettingsAttribute(string path, string label)
    {
        Path = path;
        Label = label;
    }
}
