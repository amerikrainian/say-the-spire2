using System;
using System.Diagnostics;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;

namespace SayTheSpire2.Events;

public static class EventDispatcher
{
    public static bool VerboseLogging { get; set; } = true;

    public static void Enqueue(GameEvent evt)
    {
        var message = evt.GetMessage();
        if (string.IsNullOrEmpty(message)) return;

        if (VerboseLogging)
        {
            var caller = new StackTrace(1, false);
            var callerFrame = caller.GetFrame(0);
            var callerMethod = callerFrame?.GetMethod();
            var callerInfo = callerMethod != null
                ? $"{callerMethod.DeclaringType?.Name}.{callerMethod.Name}"
                : "unknown";
            Log.Info($"[EventDebug] Enqueue: type={evt.GetType().Name} caller={callerInfo} msg=\"{message}\"");
        }

        var attr = (EventSettingsAttribute?)Attribute.GetCustomAttribute(
            evt.GetType(), typeof(EventSettingsAttribute));

        bool announce = attr != null ? EventRegistry.ShouldAnnounce(attr.Key) : evt.ShouldAnnounce();
        bool buffer = attr != null ? EventRegistry.ShouldBuffer(attr.Key) : evt.ShouldAddToBuffer();

        if (VerboseLogging)
        {
            Log.Info($"[EventDebug]   announce={announce} buffer={buffer} settingsKey={attr?.Key ?? "none"}");
        }

        if (announce)
        {
            SpeechManager.Output(Message.Raw(message), interrupt: false);
        }

        if (buffer)
        {
            var buf = BufferManager.Instance.GetBuffer("events");
            buf?.Add(message);
            BufferManager.Instance.EnableBuffer("events", true);
        }
    }
}
