using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bonfire.ViewModels;

namespace Bonfire.Views;

/// <summary>
///     Логика взаимодействия для SeedlingsWindow.xaml
/// </summary>
public partial class SeedlingsWindow : UserControl
{
    private readonly HashSet<ValidationError> _editorErrors = [];

    public SeedlingsWindow()
    {
        InitializeComponent();
    }

    private void OnEditorValidationError(object sender, ValidationErrorEventArgs e)
    {
        if (e.Action == ValidationErrorEventAction.Added) _editorErrors.Add(e.Error);
        else _editorErrors.Remove(e.Error);
        if (DataContext is SeedlingsViewModel vm) vm.EditorHasBindingErrors = _editorErrors.Count > 0;
    }

    private void OnEditorUnloaded(object sender, RoutedEventArgs e)
    {
        _editorErrors.Clear();
        if (DataContext is SeedlingsViewModel vm) vm.EditorHasBindingErrors = false;
    }

    private void ComboBox_OnKeyUp(object sender, KeyEventArgs e)
    {
        if (sender is UserControl control && e.Key == Key.Escape &&
            (DataGrid)(control.Content as Grid)?.Children[0]! != null)
        {
            var dataGrid = (DataGrid)(control.Content as Grid)!.Children[0];
            dataGrid.SelectedIndex = -1;
            dataGrid.UnselectAllCells();
        }

        if (sender is not ComboBox box || e.Key != Key.Enter) return;
        box.Focus();
        if (Keyboard.PrimaryDevice == null) return;
        if (Keyboard.PrimaryDevice.ActiveSource == null) return;
        var e1 = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0,
                Key.Tab)
            { RoutedEvent = Keyboard.KeyDownEvent };
        InputManager.Current.ProcessInput(e1);
    }

    private void TextBlock_OnKeyUp(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || e.Key != Key.Enter) return;
        box.Focus();
        if (Keyboard.PrimaryDevice == null) return;
        if (Keyboard.PrimaryDevice.ActiveSource == null) return;
        var e1 = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0,
                Key.Tab)
            { RoutedEvent = Keyboard.KeyDownEvent };
        InputManager.Current.ProcessInput(e1);
    }
}