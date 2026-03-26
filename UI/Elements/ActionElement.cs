using System;

namespace SayTheSpire2.UI.Elements;

public class ActionElement : UIElement
{
    private readonly Func<string?> _label;
    private readonly Func<string?>? _extras;
    private readonly Func<string?>? _typeKey;
    private readonly Func<string?>? _status;
    private readonly Func<string?>? _tooltip;
    private readonly Func<bool>? _isVisible;
    private readonly Action? _onActivated;

    public ActionElement(
        Func<string?> label,
        Func<string?>? status = null,
        Func<string?>? tooltip = null,
        Func<string?>? typeKey = null,
        Func<string?>? extras = null,
        Func<bool>? isVisible = null,
        Action? onActivated = null)
    {
        _label = label;
        _status = status;
        _tooltip = tooltip;
        _typeKey = typeKey;
        _extras = extras;
        _isVisible = isVisible;
        _onActivated = onActivated;
    }

    public override bool IsVisible => _isVisible?.Invoke() ?? true;

    public override string? GetLabel() => _label();
    public override string? GetExtrasString() => _extras?.Invoke();
    public override string? GetTypeKey() => _typeKey?.Invoke();
    public override string? GetStatusString() => _status?.Invoke();
    public override string? GetTooltip() => _tooltip?.Invoke();

    public bool Activate()
    {
        if (_onActivated == null)
            return false;

        _onActivated();
        return true;
    }
}
