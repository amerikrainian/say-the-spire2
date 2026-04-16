using System;
using System.Collections.Generic;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public abstract class UIElement
{
    public Container? Parent { get; set; }

    public virtual bool IsVisible => true;

    public abstract Message? GetLabel();
    public virtual Message? GetExtrasString() => null;
    public virtual string? GetTypeKey() => null;
    public virtual string? GetSubtypeKey() => null;
    public virtual Message? GetStatusString() => null;
    public virtual Message? GetTooltip() => null;

    /// <summary>
    /// Fired during focus string building to collect additional pre-type extras.
    /// Handlers append messages to the provided list.
    /// </summary>
    public event Action<List<Message>>? CollectPreExtras;

    /// <summary>
    /// Fired during focus string building to collect additional post-type extras.
    /// Handlers append messages to the provided list.
    /// </summary>
    public event Action<List<Message>>? CollectPostExtras;

    /// <summary>
    /// Called when this element receives focus. Configure which buffers are enabled
    /// and populate them with data. Return the key of the buffer to set as current,
    /// or null to keep the default "ui" buffer.
    /// </summary>
    public virtual string? HandleBuffers(BufferManager buffers)
    {
        // Default: populate the UI buffer with label and status
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();
            var label = GetLabel()?.Resolve();
            if (!string.IsNullOrEmpty(label))
                uiBuffer.Add(label);
            var status = GetStatusString()?.Resolve();
            if (!string.IsNullOrEmpty(status))
                uiBuffer.Add(status);
            var tooltip = GetTooltip()?.Resolve();
            if (!string.IsNullOrEmpty(tooltip))
                uiBuffer.Add(tooltip);
            buffers.EnableBuffer("ui", true);
        }
        return "ui";
    }

    public bool IsFocused { get; private set; }

    public void Focus()
    {
        IsFocused = true;
        OnFocus();
    }

    public void Unfocus()
    {
        IsFocused = false;
        OnUnfocus();
    }

    public virtual void Update()
    {
        OnUpdate();
    }

    protected virtual void OnFocus() { }
    protected virtual void OnUnfocus() { }
    protected virtual void OnUpdate() { }

    /// <summary>
    /// Builds the spoken focus message by composing the announcements yielded by
    /// GetFocusAnnouncements. Unmigrated proxies rely on the default shim which
    /// surfaces the old GetLabel/GetExtrasString/GetTypeKey/GetSubtypeKey/
    /// GetStatusString/GetTooltip output as one LegacyAnnouncement.
    /// </summary>
    public Message GetFocusMessage() =>
        AnnouncementComposer.Compose(this, GetFocusAnnouncements());

    /// <summary>
    /// Yields the announcements that make up this element's focus message.
    /// Default implementation returns a single LegacyAnnouncement wrapping the
    /// old focus-string output. Migrated proxies override this to yield
    /// structured announcement instances directly.
    /// </summary>
    public virtual IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var legacy = BuildLegacyFocusMessage();
        if (legacy != null && !legacy.IsEmpty)
            yield return new LegacyAnnouncement(legacy);
    }

    private Message BuildLegacyFocusMessage()
    {
        var parts = new List<Message>();

        var labelPart = BuildLabelPart();
        if (labelPart != null)
            parts.Add(labelPart);

        var typePart = BuildTypePart();
        if (typePart != null)
            parts.Add(typePart);

        var postExtras = new List<Message>();
        CollectPostExtras?.Invoke(postExtras);
        foreach (var extra in postExtras)
            parts.Add(extra);

        var tooltip = GetTooltip();
        if (tooltip != null)
        {
            var tk = GetTypeKey();
            if (string.IsNullOrEmpty(tk) || FocusStringSettings.ShouldAnnounceTooltip(tk))
                parts.Add(tooltip);
        }

        return parts.Count > 0 ? Message.Join(", ", parts.ToArray()) : Message.Empty;
    }

    /// <summary>Back-compat: resolve the focus message to a string.</summary>
    public string GetFocusString() => GetFocusMessage().Resolve();

    private Message? BuildLabelPart()
    {
        var label = GetLabel();
        var preExtras = new List<Message>();
        var extras = GetExtrasString();
        if (extras != null)
            preExtras.Add(extras);
        CollectPreExtras?.Invoke(preExtras);

        if (label == null && preExtras.Count == 0)
            return null;

        if (preExtras.Count == 0)
            return label;

        var extrasPart = Message.Join(", ", preExtras.ToArray());
        return label != null ? label + extrasPart : extrasPart;
    }

    private Message? BuildTypePart()
    {
        var parts = new List<Message>();
        var typeKey = GetTypeKey();

        var subtypeKey = GetSubtypeKey();
        if (!string.IsNullOrEmpty(subtypeKey)
            && (string.IsNullOrEmpty(typeKey) || FocusStringSettings.ShouldAnnounceSubtype(typeKey)))
        {
            parts.Add(Message.Localized("ui", $"TYPES.{subtypeKey.ToUpperInvariant()}"));
        }

        if (!string.IsNullOrEmpty(typeKey) && FocusStringSettings.ShouldAnnounceType(typeKey))
        {
            parts.Add(Message.Localized("ui", $"TYPES.{typeKey.ToUpperInvariant()}"));
        }

        var status = GetStatusString();
        if (status != null)
            parts.Add(status);

        return parts.Count > 0 ? Message.Join(" ", parts.ToArray()) : null;
    }
}
