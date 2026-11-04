using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Bonfire.Infrastructure.Behaviors;

/// <summary>Прокрутка длинного текста в закрытом editable-подборе без изменения его значения.</summary>
public static class OverflowTextScroll
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(OverflowTextScroll), new PropertyMetadata(false, Changed));
    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State", typeof(State), typeof(OverflowTextScroll));
    private static readonly DependencyProperty OffsetProperty = DependencyProperty.RegisterAttached(
        "Offset", typeof(double), typeof(OverflowTextScroll), new PropertyMetadata(0d, OffsetChanged));

    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);

    private static void OffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is TextBox editor && !editor.IsKeyboardFocusWithin)
            editor.ScrollToHorizontalOffset((double)args.NewValue);
    }

    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not ComboBox box) return;
        if (box.GetValue(StateProperty) is State previous) previous.Detach();
        box.ClearValue(StateProperty);
        if (!(bool)args.NewValue) return;
        var state = new State(box);
        box.SetValue(StateProperty, state);
        state.Attach();
    }

    private sealed class State(ComboBox box)
    {
        private TextBox? _editor;
        private bool _loaded;
        private bool _pending;

        public void Attach()
        {
            box.Loaded += Load;
            box.Unloaded += Unload;
            if (box.IsLoaded) Load(box, new RoutedEventArgs());
        }

        public void Detach()
        {
            Unload(box, new RoutedEventArgs());
            box.Loaded -= Load;
            box.Unloaded -= Unload;
        }

        private void Load(object sender, RoutedEventArgs args)
        {
            if (_loaded) return;
            _loaded = true;
            box.SizeChanged += SizeChanged;
            box.IsKeyboardFocusWithinChanged += FocusChanged;
            box.DropDownOpened += DropDownChanged;
            box.DropDownClosed += DropDownChanged;
            box.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(TextChanged));
            QueueRefresh();
        }

        private void Unload(object sender, RoutedEventArgs args)
        {
            if (!_loaded) return;
            _loaded = false;
            box.SizeChanged -= SizeChanged;
            box.IsKeyboardFocusWithinChanged -= FocusChanged;
            box.DropDownOpened -= DropDownChanged;
            box.DropDownClosed -= DropDownChanged;
            box.RemoveHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(TextChanged));
            _editor?.BeginAnimation(OffsetProperty, null);
            _editor = null;
        }

        private void SizeChanged(object sender, SizeChangedEventArgs args) => QueueRefresh();
        private void FocusChanged(object sender, DependencyPropertyChangedEventArgs args) => QueueRefresh();
        private void DropDownChanged(object? sender, EventArgs args) => QueueRefresh();
        private void TextChanged(object sender, TextChangedEventArgs args) => QueueRefresh();

        private void QueueRefresh()
        {
            if (!_loaded || _pending) return;
            _pending = true;
            box.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                _pending = false;
                if (_loaded) Refresh();
            }));
        }

        private void Refresh()
        {
            _editor?.BeginAnimation(OffsetProperty, null);
            box.ApplyTemplate();
            _editor = box.Template?.FindName("PART_EditableTextBox", box) as TextBox;
            if (_editor is null || box.IsKeyboardFocusWithin || box.IsDropDownOpen) return;
            _editor.UpdateLayout();
            _editor.ScrollToHorizontalOffset(0);
            var distance = _editor.ExtentWidth - _editor.ViewportWidth;
            if (distance <= 1) return;
            var travel = distance / 25;
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(2 * travel + 2),
                RepeatBehavior = RepeatBehavior.Forever
            };
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(distance, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(travel + 1))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(distance, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(travel + 2))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2 * travel + 2))));
            _editor.BeginAnimation(OffsetProperty, animation);
        }
    }
}
