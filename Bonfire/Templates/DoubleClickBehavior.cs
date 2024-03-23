using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Bonfire.Templates;

public class DoubleClickBehavior : Behavior<DataGrid>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseDoubleClick +=
            new MouseButtonEventHandler(OnDataGridMouseDoubleClick);
    }
    protected override void OnDetaching()
    {
        AssociatedObject.MouseDoubleClick -=
            new MouseButtonEventHandler(OnDataGridMouseDoubleClick);
        base.OnDetaching();
    }

    private async void OnDataGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid) return;
            

        if (grid.Dispatcher.CheckAccess())
        {
            Action();
        }
        else
        {
            await grid.Dispatcher.BeginInvoke((Action?)Action);
        }

        return;

        void Action()
        {
            var row = GetDataGridRowUnderMouse(e);
            if (row != null)
                row.DetailsVisibility = row.DetailsVisibility == Visibility.Collapsed ?
                Visibility.Visible : Visibility.Collapsed;

        }

        DataGridRow? GetDataGridRowUnderMouse(MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            while (originalSource != null && originalSource is not DataGridRow)
            {
                originalSource = System.Windows.Media.VisualTreeHelper.GetParent(originalSource);
            }
            return originalSource as DataGridRow;
        }
    }
}