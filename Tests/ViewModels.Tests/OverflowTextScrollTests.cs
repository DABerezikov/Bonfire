using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Bonfire.Infrastructure.Behaviors;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public class OverflowTextScrollTests(WpfRuntime runtime)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverflowScrollsWithoutChangingTextAndPausesForEditing(bool overflow) => runtime.Invoke(() =>
    {
        var text = overflow ? "Томат Ваше благородие F1 — очень длинное название сорта" : "Черри";
        var source = new TextBox { Text = text };
        var box = new ComboBox { IsEditable = true, Width = 150, IsSynchronizedWithCurrentItem = false };
        box.SetBinding(ComboBox.TextProperty, new Binding(nameof(TextBox.Text))
            { Source = source, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        SuggestionInput.SetEnabled(box, true);
        OverflowTextScroll.SetEnabled(box, true);
        var other = new Button { Content = "Другое поле" };
        var panel = new StackPanel(); panel.Children.Add(box); panel.Children.Add(other);
        var window = new Window { Content = panel, Width = 450, Height = 150,
            Left = -10000, Top = -10000, ShowInTaskbar = false };
        try
        {
            window.Show(); Keyboard.Focus(other); WpfRuntime.Layout(panel, 450, 150);
            var editor = (TextBox)box.Template.FindName("PART_EditableTextBox", box);
            Assert.Equal(overflow, editor.HasAnimatedProperties);
            if (overflow)
            {
                var frame = new DispatcherFrame();
                var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(1600) };
                timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
                timer.Start(); Dispatcher.PushFrame(frame);
                Assert.True(editor.HorizontalOffset > 0);
                Assert.Equal(text, source.Text); Assert.Equal(text, box.Text);
                window.Content = null; WpfRuntime.Layout(panel, 450, 150);
                Assert.False(editor.HasAnimatedProperties);
                window.Content = panel; Keyboard.Focus(other); WpfRuntime.Layout(panel, 450, 150);
                Assert.True(editor.HasAnimatedProperties);
                box.Width = 900; window.Width = 1000; WpfRuntime.Layout(panel, 1000, 150);
                Assert.False(editor.HasAnimatedProperties); Assert.Equal(0, editor.HorizontalOffset);
                box.Width = 150; window.Width = 450; WpfRuntime.Layout(panel, 450, 150);
                Assert.True(editor.HasAnimatedProperties);
                box.IsDropDownOpen = true; WpfRuntime.Layout(panel, 450, 150);
                Assert.False(editor.HasAnimatedProperties);
                box.IsDropDownOpen = false; Keyboard.Focus(other); WpfRuntime.Layout(panel, 450, 150);
                Assert.True(editor.HasAnimatedProperties);
                OverflowTextScroll.SetEnabled(box, false);
                Assert.False(editor.HasAnimatedProperties);
                OverflowTextScroll.SetEnabled(box, true); WpfRuntime.Layout(panel, 450, 150);
                Assert.True(editor.HasAnimatedProperties);
            }
            Keyboard.Focus(editor); WpfRuntime.Layout(panel, 450, 150);
            Assert.False(editor.HasAnimatedProperties);
            editor.Text = "Новый сорт"; editor.Select(2, 3); WpfRuntime.Layout(panel, 450, 150);
            Assert.Equal("Новый сорт", source.Text);
            Assert.Equal(2, editor.SelectionStart); Assert.Equal(3, editor.SelectionLength);
            Assert.Equal(BindingStatus.Active, box.GetBindingExpression(ComboBox.TextProperty)!.Status);
            Keyboard.Focus(other); WpfRuntime.Layout(panel, 450, 150);
            Assert.False(editor.HasAnimatedProperties);
        }
        finally { window.Close(); }
    });
}
