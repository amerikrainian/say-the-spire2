namespace SayTheSpire2.Events;

public abstract class GameEvent
{
    public abstract string? GetMessage();

    public virtual bool ShouldAnnounce() => true;

    public virtual bool ShouldAddToBuffer() => true;
}
