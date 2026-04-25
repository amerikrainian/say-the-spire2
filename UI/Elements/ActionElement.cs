using System;
using System.Collections.Generic;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(StatusAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ActionElement : UIElement
{
    private readonly Func<Message?> _label;
    private readonly Func<string?>? _typeKey;
    private readonly Func<Message?>? _status;
    private readonly Func<Message?>? _tooltip;
    private readonly Func<bool>? _isVisible;
    private readonly Action? _onActivated;

    public ActionElement(
        Func<Message?> label,
        Func<Message?>? status = null,
        Func<Message?>? tooltip = null,
        Func<string?>? typeKey = null,
        Func<bool>? isVisible = null,
        Action? onActivated = null)
    {
        _label = label;
        _status = status;
        _tooltip = tooltip;
        _typeKey = typeKey;
        _isVisible = isVisible;
        _onActivated = onActivated;
    }

    public override bool IsVisible => _isVisible?.Invoke() ?? true;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = _label();
        if (label is { IsEmpty: false })
            yield return new LabelAnnouncement(label);

        var typeKey = _typeKey?.Invoke();
        if (!string.IsNullOrEmpty(typeKey))
            yield return new TypeAnnouncement(typeKey);

        var status = _status?.Invoke();
        if (status is { IsEmpty: false })
            yield return new StatusAnnouncement(status);

        var tooltip = _tooltip?.Invoke();
        if (tooltip is { IsEmpty: false })
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel() => _label();
    public override string? GetTypeKey() => _typeKey?.Invoke();
    public override Message? GetStatusString() => _status?.Invoke();
    public override Message? GetTooltip() => _tooltip?.Invoke();

    public bool Activate()
    {
        if (_onActivated == null)
            return false;

        _onActivated();
        return true;
    }
}
