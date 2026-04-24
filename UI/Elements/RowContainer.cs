namespace SayTheSpire2.UI.Elements;

/// <summary>
/// A horizontal list of focusable children treated as a single row by an outer
/// <see cref="NavigableContainer"/>. The outer navigation handles up/down
/// (between rows) and accept (activate the row's current child); left/right
/// move within the row. Tracks its own focused child by reference so parent
/// reorders don't shift focus off the intended element.
///
/// Announces itself transparently — when the outer navigates onto a row,
/// <see cref="FocusCurrent"/> pushes focus to the row's current child so the
/// announced text describes the button, not the row wrapper.
/// </summary>
public class RowContainer : ListContainer
{
    private UIElement? _focusedChild;

    public UIElement? FocusedChild =>
        _focusedChild != null && IndexOf(_focusedChild) >= 0 ? _focusedChild : null;

    /// <summary>
    /// Focus the row's current child (or the first visible one if none is
    /// remembered). Called by the outer NavigableContainer when the row
    /// becomes the active row.
    /// </summary>
    public void FocusCurrent()
    {
        var target = FocusedChild ?? FirstVisible();
        if (target == null) return;
        _focusedChild = target;
        UIManager.SetFocusedElement(target);
    }

    /// <summary>
    /// Focus a specific child of this row. Used by the outer NavigableContainer
    /// when the focus target is a leaf inside us (e.g., mouse click on one of
    /// the row's buttons).
    /// </summary>
    public void SetFocusTo(UIElement child)
    {
        if (IndexOf(child) < 0 || !child.IsVisible) return;
        _focusedChild = child;
        UIManager.SetFocusedElement(child);
    }

    public bool MoveRelative(int direction)
    {
        if (Children.Count == 0) return false;

        int index = _focusedChild != null ? IndexOf(_focusedChild) : -1;
        while (true)
        {
            index += direction;
            if (index < 0 || index >= Children.Count)
                return true; // boundary — consume but do nothing

            if (Children[index].IsVisible)
            {
                _focusedChild = Children[index];
                UIManager.SetFocusedElement(_focusedChild);
                return true;
            }
        }
    }

    public bool ActivateFocused()
    {
        var child = FocusedChild;
        if (child is ButtonElement button)
        {
            button.Activate();
            return true;
        }
        return false;
    }

    private UIElement? FirstVisible()
    {
        foreach (var c in Children)
            if (c.IsVisible) return c;
        return null;
    }
}
