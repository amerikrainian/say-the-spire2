namespace Sts2AccessibilityMod.Input;

public abstract class InputContext
{
    /// <summary>
    /// Called when an action is first pressed. Return true to consume (stop propagation).
    /// </summary>
    public virtual bool OnActionJustPressed(InputAction action) => false;

    /// <summary>
    /// Called on repeated key events while held. Return true to consume.
    /// </summary>
    public virtual bool OnActionPressed(InputAction action) => false;

    /// <summary>
    /// Called when an action is released. Return true to consume.
    /// </summary>
    public virtual bool OnActionJustReleased(InputAction action) => false;

    public virtual void OnPush() { }
    public virtual void OnPop() { }
    public virtual void OnFocus() { }
    public virtual void OnUnfocus() { }
}
