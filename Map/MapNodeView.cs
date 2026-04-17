using System.Collections.Generic;

namespace SayTheSpire2.Map;

/// <summary>
/// Structured snapshot of a map node's announceable data. Produced by
/// <see cref="MapNodeAnnouncementFormatter.BuildView"/>. Consumers either
/// render it to a single string (via DescribeNode, for one-shot speech)
/// or turn it into discrete Announcement instances (via ProxyMapPoint.
/// GetFocusAnnouncements, for focus strings).
/// </summary>
public sealed record MapNodeView(
    string TypeName,
    string Coordinates,
    string? State,
    bool IsMarked,
    bool IsFreeTravel,
    IReadOnlyList<string> OnPathMarkers,
    IReadOnlyList<string> DivergingMarkers,
    IReadOnlyList<string> Voters,
    bool IsChoice);
