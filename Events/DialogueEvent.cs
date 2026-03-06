namespace SayTheSpire2.Events;

public class DialogueEvent : GameEvent
{
    private readonly string? _speaker;
    private readonly string _text;

    public DialogueEvent(string? speaker, string text)
    {
        _speaker = speaker;
        _text = text;
    }

    public override string? GetMessage()
    {
        if (string.IsNullOrEmpty(_text)) return null;
        if (string.IsNullOrEmpty(_speaker)) return _text;
        return $"{_speaker}: {_text}";
    }

    public override bool ShouldAddToBuffer() => false;
}
