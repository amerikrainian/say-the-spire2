using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Buffers;

public class Buffer
{
    private readonly List<string> _contents = new();

    public string Key { get; set; }
    public bool Enabled { get; set; }
    public int Position { get; private set; }

    public Buffer(string key)
    {
        Key = key;
    }

    public string? Label
    {
        get
        {
            var localized = LocalizationManager.Get("ui", $"BUFFERS.{Key.ToUpperInvariant()}");
            return localized ?? Key;
        }
    }

    public void Add(string item)
    {
        if (item != null)
            _contents.Add(item);
    }

    public void Clear()
    {
        _contents.Clear();
        Position = 0;
    }

    public bool IsEmpty => _contents.Count == 0;

    public int Count => _contents.Count;

    public string? CurrentItem
    {
        get
        {
            if (_contents.Count == 0) return null;
            if (Position >= _contents.Count) Position = 0;
            return _contents[Position];
        }
    }

    public bool MoveToNext()
    {
        Update();
        if (Position + 1 >= _contents.Count) return false;
        Position++;
        return true;
    }

    public bool MoveToPrevious()
    {
        Update();
        if (Position - 1 < 0) return false;
        Position--;
        return true;
    }

    public bool MoveToPosition(int position)
    {
        if (position < 0 || position >= _contents.Count) return false;
        Position = position;
        return true;
    }

    /// <summary>
    /// Called when the buffer becomes active or is navigated.
    /// Override in subclasses to refresh contents from a bound game object.
    /// </summary>
    public virtual void Update()
    {
    }
}
