using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using Sts2AccessibilityMod.UI.Elements;

namespace Sts2AccessibilityMod.UI.Screens;

public abstract class GameScreen : Screen
{
    private readonly Dictionary<Control, UIElement> _registry = new();

    public override void OnPush()
    {
        _registry.Clear();
        BuildRegistry();
        Log.Info($"[AccessibilityMod] Screen opened: {ScreenName} ({_registry.Count} controls registered)");
    }

    public override void OnPop()
    {
        _registry.Clear();
    }

    public override UIElement? GetElement(Control control)
    {
        return _registry.TryGetValue(control, out var element) ? element : null;
    }

    protected void Register(Control control, UIElement element)
    {
        _registry[control] = element;
    }

    protected abstract void BuildRegistry();
}
