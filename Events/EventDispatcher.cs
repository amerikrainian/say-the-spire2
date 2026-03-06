using SayTheSpire2.Buffers;
using SayTheSpire2.Speech;

namespace SayTheSpire2.Events;

public static class EventDispatcher
{
    public static void Enqueue(GameEvent evt)
    {
        var message = evt.GetMessage();
        if (string.IsNullOrEmpty(message)) return;

        if (evt.ShouldAnnounce())
        {
            SpeechManager.Output(message, interrupt: false);
        }

        if (evt.ShouldAddToBuffer())
        {
            var buffer = BufferManager.Instance.GetBuffer("events");
            buffer?.Add(message);
            BufferManager.Instance.EnableBuffer("events", true);
        }
    }
}
