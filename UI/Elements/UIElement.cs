using System;
using System.Collections.Generic;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

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
    /// Builds the spoken focus message in the format:
    /// {label} {extras1}, {subtype} {type} {status}, {extras2}, {tooltip}
    /// </summary>
    public Message GetFocusMessage()
    {
        var parts = new List<Message>();

        // Label + pre-type extras (space-separated from label)
        var labelPart = BuildLabelPart();
        if (labelPart != null)
            parts.Add(labelPart);

        // Subtype + type + status
        var typePart = BuildTypePart();
        if (typePart != null)
            parts.Add(typePart);

        // Post-type extras
        var postExtras = new List<Message>();
        CollectPostExtras?.Invoke(postExtras);
        foreach (var extra in postExtras)
            parts.Add(extra);

        // Tooltip
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
