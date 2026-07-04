using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// Proxy for one mod row in the settings modding screen. Announces the mod
/// name, its enabled tickbox as checkbox state, selection, and the load
/// states a sighted player reads from row color (failed / duplicate / newly
/// installed). The UI buffer carries what the info panel shows for the
/// selected mod — source, author, version, description, and load errors —
/// so the details are readable per row without selecting it.
/// </summary>
[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(SelectedMarkerAnnouncement),
    typeof(TypeAnnouncement),
    typeof(StatusAnnouncement)
)]
public class ProxyModMenuRow : ProxyElement
{
    private static readonly System.Reflection.FieldInfo TickboxField =
        AccessTools.Field(typeof(NModMenuRow), "_tickbox")!;
    private static readonly System.Reflection.FieldInfo IsSelectedField =
        AccessTools.Field(typeof(NModMenuRow), "_isSelected")!;

    public ProxyModMenuRow(Control control) : base(control) { }

    private NModMenuRow? Row => Control as NModMenuRow;
    private Mod? Mod => Row?.Mod;
    private NTickbox? Tickbox =>
        Row != null ? TickboxField.GetValue(Row) as NTickbox : null;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        var row = Row;
        if (row != null && IsSelectedField.GetValue(row) is true)
            yield return new SelectedMarkerAnnouncement();

        yield return new TypeAnnouncement("checkbox");

        var status = GetStatusString();
        if (status != null)
            yield return new StatusAnnouncement(status);
    }

    public override Message? GetLabel()
    {
        var mod = Mod;
        if (mod?.manifest?.name is { Length: > 0 } name)
            return Message.Raw(name);
        if (mod?.manifest?.id is { Length: > 0 } id)
            return Message.Raw(id);
        return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
    }

    public override string? GetTypeKey() => "checkbox";

    public override Message? GetStatusString()
    {
        var parts = new List<Message>();

        var tickbox = Tickbox;
        if (tickbox != null)
            parts.Add(Message.Localized("ui", tickbox.IsTicked ? "CHECKBOX.CHECKED" : "CHECKBOX.UNCHECKED"));

        var state = GetLoadStateMessage();
        if (state != null)
            parts.Add(state);

        if (parts.Count == 0) return null;
        return Message.Join(", ", parts.ToArray());
    }

    /// <summary>
    /// The load states a sighted player reads from the row color. Loaded and
    /// plain Disabled are omitted — the tickbox state already covers them.
    /// </summary>
    private Message? GetLoadStateMessage() => Mod?.state switch
    {
        ModLoadState.Failed => Message.Localized("ui", "MODS.STATE_FAILED"),
        ModLoadState.DisabledDuplicate => Message.Localized("ui", "MODS.STATE_DUPLICATE"),
        ModLoadState.AddedAtRuntime => Message.Localized("ui", "MODS.STATE_NEW"),
        _ => null,
    };

    public static bool IsRowSelected(NModMenuRow row) =>
        IsSelectedField.GetValue(row) is true;

    /// <summary>
    /// The details the info panel offers for this mod: source, author,
    /// version, description, then any load errors.
    /// </summary>
    public List<string> GetDetailItems()
    {
        var items = new List<string>();
        var mod = Mod;
        if (mod == null) return items;

        var source = Message.Localized("ui", mod.modSource == ModSource.SteamWorkshop
            ? "MODS.SOURCE_WORKSHOP"
            : "MODS.SOURCE_FOLDER").Resolve();
        if (!string.IsNullOrEmpty(source))
            items.Add(source);

        if (mod.manifest?.author is { Length: > 0 } author)
            items.Add(Message.Localized("ui", "MODS.AUTHOR", new { author }).Resolve());
        if (mod.manifest?.version is { Length: > 0 } version)
            items.Add(Message.Localized("ui", "MODS.VERSION", new { version }).Resolve());
        if (mod.manifest?.description is { Length: > 0 } description)
            items.Add(StripBbcode(description));

        if (mod.errors != null)
        {
            foreach (var error in mod.errors)
                items.Add(StripBbcode(error.GetFormattedText()));
        }

        return items;
    }

    /// <summary>
    /// One UI-buffer item per detail the info panel offers: name and state,
    /// then <see cref="GetDetailItems"/>.
    /// </summary>
    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer == null || Mod == null) return base.HandleBuffers(buffers);

        uiBuffer.Clear();

        var label = GetLabel()?.Resolve();
        var status = GetStatusString()?.Resolve();
        if (!string.IsNullOrEmpty(label))
            uiBuffer.Add(string.IsNullOrEmpty(status) ? label : $"{label}, {status}");

        foreach (var item in GetDetailItems())
            uiBuffer.Add(item);

        buffers.EnableBuffer("ui", true);
        return "ui";
    }

    protected override void OnFocus()
    {
        var tickbox = Tickbox;
        if (tickbox != null)
            tickbox.Toggled += OnTickboxToggled;
    }

    protected override void OnUnfocus()
    {
        var tickbox = Tickbox;
        if (tickbox != null)
            tickbox.Toggled -= OnTickboxToggled;
    }

    private void OnTickboxToggled(NTickbox tickbox)
    {
        SpeechManager.Output(Message.Localized("ui",
            tickbox.IsTicked ? "CHECKBOX.CHECKED" : "CHECKBOX.UNCHECKED"));
    }
}
