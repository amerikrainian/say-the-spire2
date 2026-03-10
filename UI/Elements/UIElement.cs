using System;
using System.Collections.Generic;
using System.Text;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Elements;

public abstract class UIElement
{
    public Container? Parent { get; set; }

    public virtual bool IsVisible => true;

    public abstract string? GetLabel();
    public virtual string? GetExtrasString() => null;
    public virtual string? GetTypeKey() => null;
    public virtual string? GetSubtypeKey() => null;
    public virtual string? GetStatusString() => null;
    public virtual string? GetTooltip() => null;

    /// <summary>
    /// Fired during focus string building to collect additional pre-type extras.
    /// Handlers append strings to the provided list.
    /// </summary>
    public event Action<List<string>>? CollectPreExtras;

    /// <summary>
    /// Fired during focus string building to collect additional post-type extras.
    /// Handlers append strings to the provided list.
    /// </summary>
    public event Action<List<string>>? CollectPostExtras;

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
            var label = GetLabel();
            if (!string.IsNullOrEmpty(label))
                uiBuffer.Add(label);
            var status = GetStatusString();
            if (!string.IsNullOrEmpty(status))
                uiBuffer.Add(status);
            var tooltip = GetTooltip();
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
    /// Builds the spoken focus string in the format:
    /// {label} {extras1}, {subtype} {type} {status}, {extras2}, {tooltip}
    /// </summary>
    public string GetFocusString()
    {
        var sb = new StringBuilder();

        // Label
        var label = GetLabel();
        if (!string.IsNullOrEmpty(label))
            sb.Append(label);

        // Pre-type extras: element's own + collected from hooks
        var preExtras = new List<string>();
        var extras = GetExtrasString();
        if (!string.IsNullOrEmpty(extras))
            preExtras.Add(extras);
        CollectPreExtras?.Invoke(preExtras);
        if (preExtras.Count > 0)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(string.Join(", ", preExtras));
        }

        // Subtype + type + status
        var typePart = BuildTypePart();
        if (!string.IsNullOrEmpty(typePart))
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(typePart);
        }

        // Post-type extras: collected from hooks
        var postExtras = new List<string>();
        CollectPostExtras?.Invoke(postExtras);
        if (postExtras.Count > 0)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(string.Join(", ", postExtras));
        }

        // Tooltip (respects per-type setting)
        var tooltip = GetTooltip();
        if (!string.IsNullOrEmpty(tooltip))
        {
            var tk = GetTypeKey();
            if (string.IsNullOrEmpty(tk) || FocusStringSettings.ShouldAnnounceTooltip(tk))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(tooltip);
            }
        }

        return sb.Length > 0 ? sb.ToString() : "";
    }

    private string? BuildTypePart()
    {
        var sb = new StringBuilder();
        var typeKey = GetTypeKey();

        var subtypeKey = GetSubtypeKey();
        if (!string.IsNullOrEmpty(subtypeKey)
            && (string.IsNullOrEmpty(typeKey) || FocusStringSettings.ShouldAnnounceSubtype(typeKey)))
        {
            var subtypeName = Message.Localized("ui", $"TYPES.{subtypeKey.ToUpperInvariant()}").Resolve();
            if (!string.IsNullOrEmpty(subtypeName))
                sb.Append(subtypeName);
        }

        if (!string.IsNullOrEmpty(typeKey) && FocusStringSettings.ShouldAnnounceType(typeKey))
        {
            var typeName = Message.Localized("ui", $"TYPES.{typeKey.ToUpperInvariant()}").Resolve();
            if (!string.IsNullOrEmpty(typeName))
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(typeName);
            }
        }

        var status = GetStatusString();
        if (!string.IsNullOrEmpty(status))
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(status);
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }
}
