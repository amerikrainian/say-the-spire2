using System;
using System.Collections.Generic;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public sealed class StatsValueElement : UIElement
{
    private readonly Func<string?> _label;
    private readonly Func<IReadOnlyList<string>> _values;
    private int _valueIndex;
    private bool _suppressLabelForCurrentAnnouncement;

    public StatsValueElement(Func<string?> label, Func<IReadOnlyList<string>> values)
    {
        _label = label;
        _values = values;
    }

    public override string? GetLabel()
    {
        if (_suppressLabelForCurrentAnnouncement)
            return null;

        return _label();
    }

    public override string? GetStatusString()
    {
        var values = GetNormalizedValues();
        if (values.Count == 0)
            return null;

        if (_valueIndex >= values.Count)
            _valueIndex = values.Count - 1;

        return values[_valueIndex];
    }

    public bool MoveValue(int delta)
    {
        var values = GetNormalizedValues();
        if (values.Count <= 1)
            return false;

        var next = Math.Clamp(_valueIndex + delta, 0, values.Count - 1);
        if (next == _valueIndex)
            return false;

        _valueIndex = next;
        _suppressLabelForCurrentAnnouncement = true;
        return true;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
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
            buffers.EnableBuffer("ui", true);
        }

        _suppressLabelForCurrentAnnouncement = false;
        return "ui";
    }

    private IReadOnlyList<string> GetNormalizedValues()
    {
        var values = _values();
        if (values.Count == 0)
            _valueIndex = 0;
        else if (_valueIndex >= values.Count)
            _valueIndex = values.Count - 1;

        return values;
    }

    protected override void OnFocus()
    {
        _suppressLabelForCurrentAnnouncement = false;
    }

    protected override void OnUnfocus()
    {
        _suppressLabelForCurrentAnnouncement = false;
    }
}
