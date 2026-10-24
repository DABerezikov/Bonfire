using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bonfire.ViewModels;

namespace Bonfire.Views;

public partial class SeedDetailsEditor : UserControl
{
    private readonly HashSet<ValidationError> _errors = [];

    public SeedDetailsEditor() => InitializeComponent();

    private void OnValidationError(object sender, ValidationErrorEventArgs e)
    {
        if (e.Action == ValidationErrorEventAction.Added) _errors.Add(e.Error);
        else _errors.Remove(e.Error);
        if (DataContext is SeedsViewModel vm) vm.EditorHasBindingErrors = _errors.Count > 0;
        CommandManager.InvalidateRequerySuggested();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _errors.Clear();
        if (DataContext is SeedsViewModel vm) vm.EditorHasBindingErrors = false;
    }
}
