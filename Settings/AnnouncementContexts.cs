using System;

namespace SayTheSpire2.Settings;

/// <summary>
/// The surfaces an announcement setting can be exposed on. An announcement's
/// options are defined once (on its global category) but mirrored as
/// per-context overrides; this flag controls which contexts get the override.
/// A setting left at <see cref="All"/> appears everywhere — the per-context
/// registration logic only skips a setting when its flag is absent.
/// </summary>
[Flags]
public enum AnnouncementContexts
{
    None = 0,
    /// <summary>Per-element focus strings (ui.{element}.announcements.*).</summary>
    Focus = 1 << 0,
    /// <summary>Per-buffer entries (buffers.{buffer}.announcements.*).</summary>
    Buffer = 1 << 1,
    /// <summary>Per-hotkey readouts (hotkeys.{key}.*).</summary>
    Hotkey = 1 << 2,
    All = Focus | Buffer | Hotkey,
}
