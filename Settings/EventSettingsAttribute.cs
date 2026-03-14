using System;

namespace SayTheSpire2.Settings;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EventSettingsAttribute : ModSettingsAttribute
{
    public string Key { get; }
    public bool DefaultAnnounce { get; }
    public bool DefaultBuffer { get; }
    public bool HasSourceFilter { get; }
    /// <summary>
    /// Which source types the game provides visual feedback for.
    /// Only these sources will appear as toggleable options in settings,
    /// and events from other sources will be silently dropped in multiplayer.
    /// </summary>
    public bool AllowCurrentPlayer { get; }
    public bool AllowOtherPlayers { get; }
    public bool AllowEnemies { get; }

    public EventSettingsAttribute(string key, string label, bool defaultAnnounce = true, bool defaultBuffer = true,
        bool hasSourceFilter = false, bool allowCurrentPlayer = true, bool allowOtherPlayers = true, bool allowEnemies = true)
        : base($"events.{key}", $"Events/{label}")
    {
        Key = key;
        DefaultAnnounce = defaultAnnounce;
        DefaultBuffer = defaultBuffer;
        HasSourceFilter = hasSourceFilter;
        AllowCurrentPlayer = allowCurrentPlayer;
        AllowOtherPlayers = allowOtherPlayers;
        AllowEnemies = allowEnemies;
    }
}
