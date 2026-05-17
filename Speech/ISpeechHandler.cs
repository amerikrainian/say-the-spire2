using SayTheSpire2.Settings;

namespace SayTheSpire2.Speech;

public interface ISpeechHandler
{
    string Key { get; }
    string Label { get; }
    /// <summary>
    /// Optional localization key for the handler's display label. Empty means
    /// "use raw Label". Used by SpeechManager when building the handler-choice
    /// dropdown so display labels translate at runtime.
    /// </summary>
    string LocalizationKey => "";
    CategorySetting? GetSettings();
    bool Detect();
    bool Load();
    void Unload();
    bool Speak(string text, bool interrupt = false);
    bool Output(string text, bool interrupt = false);
    bool Silence();
}
