using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

namespace SayTheSpire2.UI.Elements;

public class ProxyRunHistoryMapPoint : ProxyElement
{
    private static readonly FieldInfo? EntryField =
        AccessTools.Field(typeof(NMapPointHistoryEntry), "_entry");
    private static readonly FieldInfo? QuestIconField =
        AccessTools.Field(typeof(NMapPointHistoryEntry), "_questIcon");

    public ProxyRunHistoryMapPoint(Control control) : base(control) { }

    private NMapPointHistoryEntry? EntryControl => Control as NMapPointHistoryEntry;

    public override string? GetLabel()
    {
        var control = EntryControl;
        var entry = EntryField?.GetValue(control) as MapPointHistoryEntry;
        if (control == null || entry == null)
            return null;

        var room = entry.Rooms.LastOrDefault();
        return room == null
            ? $"Floor {control.FloorNum}"
            : $"Floor {control.FloorNum}, {room.RoomType}";
    }

    public override string? GetTypeKey() => "button";

    public override string? GetStatusString()
    {
        var questIcon = QuestIconField?.GetValue(EntryControl) as Control;
        return questIcon?.Visible == true ? "Quest completed" : null;
    }
}
