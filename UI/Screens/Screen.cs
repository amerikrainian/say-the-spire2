using System.Collections.Generic;
using Godot;
using Sts2AccessibilityMod.Input;
using Sts2AccessibilityMod.UI.Elements;

namespace Sts2AccessibilityMod.UI.Screens;

public abstract class Screen
{
    public virtual string? ScreenName => null;

    // Input handling — return true to consume, false to pass to lower screens
    public virtual bool OnActionJustPressed(InputAction action) => false;
    public virtual bool OnActionPressed(InputAction action) => false;
    public virtual bool OnActionJustReleased(InputAction action) => false;

    // Lifecycle
    public virtual void OnPush() { }
    public virtual void OnPop() { }
    public virtual void OnFocus() { }
    public virtual void OnUnfocus() { }

    // Element registry — screens can optionally map controls to UI elements
    public virtual UIElement? GetElement(Control control) => null;
}
