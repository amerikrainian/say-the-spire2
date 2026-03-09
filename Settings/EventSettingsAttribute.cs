using System;

namespace SayTheSpire2.Settings;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EventSettingsAttribute : ModSettingsAttribute
{
    public string Key { get; }
    public bool DefaultAnnounce { get; }
    public bool DefaultBuffer { get; }

    public EventSettingsAttribute(string key, string label, bool defaultAnnounce = true, bool defaultBuffer = true)
        : base($"events.{key}", $"Events/{label}")
    {
        Key = key;
        DefaultAnnounce = defaultAnnounce;
        DefaultBuffer = defaultBuffer;
    }
}
