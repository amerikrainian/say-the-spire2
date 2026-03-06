namespace SayTheSpire2.Speech;

public interface ISpeechHandler
{
    string Key { get; }
    bool Detect();
    bool Load();
    void Unload();
    bool Speak(string text, bool interrupt = false);
    bool Output(string text, bool interrupt = false);
    bool Silence();
}
