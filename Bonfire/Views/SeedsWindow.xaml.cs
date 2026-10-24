using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Bonfire.ViewModels;

namespace Bonfire.Views;

/// <summary>
/// Interaction logic for SeedsWindow.xaml
/// </summary>
public partial class SeedsWindow : UserControl
{
    public SeedsWindow()
    {
        InitializeComponent();
    }

    private static T? FindParent<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source != null)
        {
            if (source is T result) return result;
            source = source is Visual ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }
        return null;
    }

    private void SeedsGrid_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not SeedsViewModel vm || sender is not DataGrid grid) return;
        var row = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row != null && !Equals(row.Item, grid.SelectedItem) && !vm.TryDiscardSeedChanges())
            e.Handled = true;
    }

    private void SeedsGrid_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (FindParent<DataGridDetailsPresenter>(e.OriginalSource as DependencyObject) != null) return;
        if (e.Key is Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown
            && DataContext is SeedsViewModel vm && !vm.TryDiscardSeedChanges()) e.Handled = true;
    }

    private void SeedsGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridDetailsPresenter>(e.OriginalSource as DependencyObject) != null) return;
        var row = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row is null || DataContext is not SeedsViewModel vm) return;
        if (row.DetailsVisibility == Visibility.Visible && !vm.TryDiscardSeedChanges()) return;
        row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        e.Handled = true;
    }

    private void SeedsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, e.OriginalSource) || sender is not DataGrid grid) return;
        foreach (var item in e.RemovedItems)
            if (grid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row)
                row.DetailsVisibility = Visibility.Collapsed;
    }



    private void ComboBox_OnKeyUp(object sender, KeyEventArgs e)
    {

        if (sender is UserControl control && e.Key == Key.Escape &&
            (DataGrid)(control.Content as Grid)?.Children[0]! != null)
        {
            if (DataContext is SeedsViewModel vm && !vm.TryDiscardSeedChanges())
            {
                e.Handled = true;
                return;
            }
            var dataGrid = (DataGrid)(control.Content as Grid)!.Children[0];
            dataGrid.SelectedIndex = -1;
            dataGrid.UnselectAllCells();
                   
        }
        if (sender is not ComboBox box || e.Key != Key.Enter) return;
        box.Focus();
        if (Keyboard.PrimaryDevice == null) return;
        if (Keyboard.PrimaryDevice.ActiveSource == null) return;
        var e1 = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0,
            Key.Tab) { RoutedEvent = Keyboard.KeyDownEvent };
        InputManager.Current.ProcessInput(e1);
    }
        
}
