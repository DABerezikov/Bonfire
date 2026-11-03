using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Bonfire.Infrastructure.Behaviors;

/// <summary>Подбор без принудительной подстановки текста.</summary>
public static class SuggestionInput
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(SuggestionInput), new PropertyMetadata(false, Changed));
    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);

    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not ComboBox box || !(bool)args.NewValue) return;
        box.IsTextSearchEnabled = false;
        var state = new State(box);
        box.Loaded += state.Load;
        box.Unloaded += state.Unload;
    }

    private sealed class State(ComboBox box)
    {
        private readonly DependencyPropertyDescriptor _property = DependencyPropertyDescriptor.FromProperty(
            ItemsControl.ItemsSourceProperty, typeof(ComboBox));
        private IEnumerable? _source;
        private ListCollectionView? _view;
        private bool _updating;
        private bool _loaded;

        public void Load(object sender, RoutedEventArgs args)
        {
            if (_loaded) return;
            _loaded = true;
            _property.AddValueChanged(box, SourceChanged);
            box.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(TextChanged));
            SourceChanged(box, EventArgs.Empty);
        }

        public void Unload(object sender, RoutedEventArgs args)
        {
            _loaded = false;
            _property.RemoveValueChanged(box, SourceChanged);
            box.RemoveHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(TextChanged));
            if (_source is INotifyCollectionChanged old) old.CollectionChanged -= CollectionChanged;
        }

        private void SourceChanged(object? sender, EventArgs args)
        {
            if (_updating) return;
            if (ReferenceEquals(box.ItemsSource, _view))
            {
                if (_source is INotifyCollectionChanged existing) existing.CollectionChanged += CollectionChanged;
                return;
            }
            if (_source is INotifyCollectionChanged old) old.CollectionChanged -= CollectionChanged;
            _source = box.ItemsSource;
            if (_source is INotifyCollectionChanged current) current.CollectionChanged += CollectionChanged;
            Rebuild();
        }

        private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (_updating) return;
            // Даём ComboBox завершить явный выбор и передать его текст в привязку.
            box.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
            {
                if (_loaded) Rebuild();
            }));
        }

        private void Rebuild()
        {
            var text = box.Text;
            _updating = true;
            try
            {
                _view = new ListCollectionView((_source?.Cast<object>() ?? []).ToList());
                if (box.IsKeyboardFocusWithin)
                    _view.Filter = item => DisplayText(item).Contains(text, StringComparison.OrdinalIgnoreCase);
                box.SetCurrentValue(ItemsControl.ItemsSourceProperty, _view);
                box.SetCurrentValue(ComboBox.TextProperty, text);
            }
            finally { _updating = false; }
        }

        private void TextChanged(object sender, TextChangedEventArgs args)
        {
            if (_updating || args.OriginalSource is not TextBox editor || !editor.IsKeyboardFocusWithin) return;
            box.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (!_loaded || !editor.IsKeyboardFocusWithin || _view is null) return;
                var text = editor.Text;
                var start = editor.SelectionStart;
                var length = editor.SelectionLength;
                _updating = true;
                try
                {
                    _view.Filter = item => DisplayText(item).Contains(text, StringComparison.OrdinalIgnoreCase);
                    box.SetCurrentValue(ComboBox.TextProperty, text);
                    editor.Text = text;
                    editor.Select(start, length);
                    box.IsDropDownOpen = _view.Count > 0;
                }
                finally { _updating = false; }
            }));
        }

        private string DisplayText(object item) => string.IsNullOrEmpty(box.DisplayMemberPath)
            ? item.ToString() ?? string.Empty
            : TypeDescriptor.GetProperties(item)[box.DisplayMemberPath]?.GetValue(item)?.ToString() ?? string.Empty;
    }
}
