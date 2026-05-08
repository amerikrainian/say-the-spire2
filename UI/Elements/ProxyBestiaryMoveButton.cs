using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(typeof(LabelAnnouncement))]
public class ProxyBestiaryMoveButton : ProxyElement
{
    public ProxyBestiaryMoveButton(Control control) : base(control) { }

    private NBestiaryMoveButton? Button => Control as NBestiaryMoveButton;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);
    }

    public override Message? GetLabel()
    {
        var button = Button;
        if (button == null)
            return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
        return Message.Raw(button.Move.displayName);
    }

    public override string? GetTypeKey() => null;
}
