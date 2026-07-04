using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using SayTheSpire2.Help;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Screen for BaseLib's mod configuration submenu: a mod list on the left and
/// the selected mod's settings on the right, rebuilt whenever a different
/// mod's config loads. Row controls reuse our existing proxies (checkbox,
/// dropdown, text input, generalized slider); each row's hover tip is
/// injected into its proxy's announcements so setting descriptions are read
/// on any control type.
///
/// BaseLib is an optional third-party mod whose versions drift independently
/// of the game and of us, so every lookup degrades gracefully (skipping the
/// affected feature) instead of crashing.
/// </summary>
public class ModConfigGameScreen : GameScreen
{
    public const string SubmenuTypeName = "BaseLib.Config.UI.NModConfigSubmenu";

    private static readonly System.Type? SubmenuType =
        AccessTools.TypeByName(SubmenuTypeName);
    private static readonly System.Reflection.FieldInfo? ModListVboxField =
        SubmenuType != null ? AccessTools.Field(SubmenuType, "_modListVbox") : null;
    private static readonly System.Reflection.FieldInfo? OptionContainerField =
        SubmenuType != null ? AccessTools.Field(SubmenuType, "_optionContainer") : null;
    private static readonly System.Reflection.FieldInfo? ModTitleField =
        SubmenuType != null ? AccessTools.Field(SubmenuType, "_modTitle") : null;

    private static readonly System.Type? RowType =
        AccessTools.TypeByName("BaseLib.Config.UI.NConfigOptionRow");
    private static readonly System.Reflection.PropertyInfo? SettingControlProperty =
        RowType != null ? AccessTools.Property(RowType, "SettingControl") : null;
    private static readonly System.Reflection.FieldInfo? RowHoverTipField =
        RowType != null ? AccessTools.Field(RowType, "_hoverTip") : null;

    private static readonly System.Type? ConfigSliderType =
        AccessTools.TypeByName("BaseLib.Config.UI.NConfigSlider");

    private static readonly System.Type? ModListButtonType =
        AccessTools.TypeByName("BaseLib.Config.UI.NModListButton");
    private static readonly System.Reflection.PropertyInfo? ModNameProperty =
        ModListButtonType != null ? AccessTools.Property(ModListButtonType, "ModName") : null;
    private static readonly System.Reflection.PropertyInfo? IsSelectedModProperty =
        ModListButtonType != null ? AccessTools.Property(ModListButtonType, "IsSelectedMod") : null;

    private readonly NSubmenu _submenu;
    private readonly ListContainer _root;
    private readonly ListContainer _modList = new()
    {
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly ListContainer _options = new()
    {
        AnnounceName = true,
        AnnouncePosition = true,
    };

    private Control? _lastOptionContainer;
    private readonly Dictionary<Node, bool> _sectionStates = new();

    public override Message? ScreenName
    {
        get
        {
            var loc = LocString.GetIfExists("settings_ui", "BASELIB-MOD_CONFIGURATION");
            return Message.Raw(loc != null
                ? ProxyElement.StripBbcode(loc.GetFormattedText())
                : "Mod Configuration");
        }
    }

    public ModConfigGameScreen(NSubmenu submenu)
    {
        _submenu = submenu;
        _root = new ListContainer
        {
            ContainerLabel = ScreenName,
            AnnounceName = true,
            AnnouncePosition = false,
        };
        // BaseLib hardcodes the list panel title as English "Mods".
        _modList.ContainerLabel = Message.Raw("Mods");
        _root.Add(_modList);
        _root.Add(_options);
        RootElement = _root;
    }

    public override List<HelpMessage> GetHelpMessages() => new()
    {
        new TextHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MOD_CONFIG",
            "Up and down move through the mod list on the left. Press confirm on a mod to open its settings, then up and down move through them. Press cancel to return to the mod list. Changes save automatically."), exclusive: true),
    };

    protected override void BuildRegistry()
    {
        BuildModList();
        BuildOptions();
    }

    public override void OnPop()
    {
        base.OnPop();
        _modList.Clear();
        _options.Clear();
        _sectionStates.Clear();
        _connectedControls.Clear();
        _lastOptionContainer = null;
    }

    public override void OnUpdate()
    {
        // Selecting a different mod recreates the option container.
        var container = OptionContainerField?.GetValue(_submenu) as Control;
        if (!ReferenceEquals(container, _lastOptionContainer))
            BuildOptions();

        AnnounceSectionToggles();
    }

    private void BuildModList()
    {
        _modList.Clear();

        if (ModListVboxField?.GetValue(_submenu) is not Godot.Container vbox)
            return;

        foreach (var child in vbox.GetChildren().OfType<Control>())
        {
            if (ModListButtonType == null || !ModListButtonType.IsInstanceOfType(child))
                continue;

            var proxy = ProxyFactory.Create(child);
            if (ModNameProperty?.GetValue(child) is string name && name.Length > 0)
                proxy.OverrideLabel = name;

            var button = child;
            proxy.CollectAnnouncements += list =>
            {
                if (IsSelectedModProperty?.GetValue(button) is true)
                    list.Add(new SelectedMarkerAnnouncement());
            };

            _modList.Add(proxy);
            Register(child, proxy);
        }
    }

    private void BuildOptions()
    {
        _options.Clear();
        _sectionStates.Clear();

        var container = OptionContainerField?.GetValue(_submenu) as Control;
        _lastOptionContainer = container;
        if (container == null)
            return;

        // Label the options group with the mod's displayed title.
        if (ModTitleField?.GetValue(_submenu) is RichTextLabel title
            && !string.IsNullOrWhiteSpace(title.Text))
        {
            _options.ContainerLabel = Message.Raw(ProxyElement.StripBbcode(title.Text));
        }

        WalkOptions(container);
    }

    /// <summary>
    /// Recursively finds config rows and collapsible sections. Sections are
    /// also recursed into — their content container holds nested rows.
    /// </summary>
    private void WalkOptions(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (RowType != null && RowType.IsInstanceOfType(child) && child is Control row)
            {
                RegisterRow(row);
                continue;
            }
            if (ProxyConfigSection.IsSection(child))
                RegisterSection(child);
            WalkOptions(child);
        }
    }

    private void RegisterRow(Control row)
    {
        if (SettingControlProperty?.GetValue(row) is not Control settingControl)
            return;

        // BaseLib's slider is a plain Control wrapper, not the game's
        // NSettingsSlider, so ProxyFactory can't route it; our generalized
        // ProxySlider reads its Slider/SliderValue children.
        var proxy = ConfigSliderType != null && ConfigSliderType.IsInstanceOfType(settingControl)
            ? new ProxySlider(settingControl)
            : ProxyFactory.Create(settingControl);

        proxy.OverrideLabel ??= FindRowLabel(row, settingControl);

        // Inject the row's hover tip (the setting's description) into the
        // proxy's announcements. This works for every proxy type — checkbox,
        // dropdown, text input, slider — without subclassing them.
        if (RowHoverTipField?.GetValue(row) is IHoverTip tip)
            proxy.CollectAnnouncements += list => list.Add(new HoverTipsAnnouncement(new[] { tip }));

        _options.Add(proxy);
        Register(settingControl, proxy);
        // Non-NClickableControl controls (slider wrapper, LineEdit) don't go
        // through the RefreshFocus hook; announce via the FocusEntered signal.
        if (settingControl is not NClickableControl)
            ConnectFocusSignal(settingControl, proxy);
    }

    private void RegisterSection(Node section)
    {
        var target = ProxyConfigSection.GetFocusTarget(section);
        if (target == null)
            return;

        var proxy = new ProxyConfigSection(target, section);
        _options.Add(proxy);
        Register(target, proxy);
        ConnectFocusSignal(target, proxy);
        _sectionStates[section] = ProxyConfigSection.IsExpanded(section);
    }

    private void AnnounceSectionToggles()
    {
        List<(Node section, bool expanded)>? changes = null;
        foreach (var (section, wasExpanded) in _sectionStates)
        {
            if (!GodotObject.IsInstanceValid(section))
                continue;
            var expanded = ProxyConfigSection.IsExpanded(section);
            if (expanded != wasExpanded)
                (changes ??= new()).Add((section, expanded));
        }
        if (changes == null)
            return;

        foreach (var (section, expanded) in changes)
        {
            _sectionStates[section] = expanded;
            SpeechManager.Output(Message.Localized("ui",
                expanded ? "STATUS.EXPANDED" : "STATUS.COLLAPSED"));
        }
    }

    private static string? FindRowLabel(Control row, Control settingControl)
    {
        foreach (var child in row.GetChildren())
        {
            if (child == settingControl)
                continue;
            var text = ProxyElement.FindChildTextPublic(child);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }
        return null;
    }
}
