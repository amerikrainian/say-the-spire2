using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A map node's current state (e.g., "traveled"). Caller supplies the pre-localized
/// state text — callers for different domains (map, timeline, etc.) can reuse this
/// class for "current state of the thing I'm focused on."
/// </summary>
public sealed class NodeStateAnnouncement : Announcement
{
    private readonly string _state;

    public NodeStateAnnouncement(string state) { _state = state; }

    public override string Key => "node_state";
    public override string Suffix => ",";
    public override Message Render() => Message.Raw(_state);
}
