using System;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Declares the canonical order in which announcements appear in an element's
/// focus message. Applied to a UIElement subclass. The composer sorts yielded
/// announcements by this list; anything yielded but not declared is appended
/// at the end in yield order. The attribute is a hint, not a contract — an
/// element can yield an announcement that isn't in the list without crashing.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class AnnouncementOrderAttribute : Attribute
{
    public Type[] Types { get; }

    public AnnouncementOrderAttribute(params Type[] types)
    {
        Types = types;
    }
}
