using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Bonfire.Templates;

public class SelectAllTextOnFocusBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.GotKeyboardFocus += OnGotKeyboardFocus;
        AssociatedObject.GotMouseCapture += OnGotMouseCapture;
        AssociatedObject.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.GotKeyboardFocus -= OnGotKeyboardFocus;
        AssociatedObject.GotMouseCapture -= OnGotMouseCapture;
        AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
    }

    private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        AssociatedObject.SelectAll();
    }

    private void OnGotMouseCapture(object sender, MouseEventArgs e)
    {
        AssociatedObject.SelectAll();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (AssociatedObject.IsKeyboardFocusWithin) return;
        AssociatedObject.Focus();
        e.Handled = true;
    }
}