using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Bonfire.Templates;

public class SelectAllTextOnFocusBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.GotKeyboardFocus += OnGotKeyboardFocus;
        this.AssociatedObject.GotMouseCapture += OnGotMouseCapture;
        this.AssociatedObject.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        this.AssociatedObject.GotKeyboardFocus -= OnGotKeyboardFocus;
        this.AssociatedObject.GotMouseCapture -= OnGotMouseCapture;
        this.AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
    }

    private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        this.AssociatedObject.SelectAll();
    }

    private void OnGotMouseCapture(object sender, MouseEventArgs e)
    {
        this.AssociatedObject.SelectAll();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (AssociatedObject.IsKeyboardFocusWithin) return;
        this.AssociatedObject.Focus();
        e.Handled = true;
    }
}