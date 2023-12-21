using System;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Bonfire.Templates
{
    public class ScrollIntoViewBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged +=
                new SelectionChangedEventHandler(AssociatedObjectSelectionChanged);
        }
        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -=
                new SelectionChangedEventHandler(AssociatedObjectSelectionChanged);
            base.OnDetaching();
        }

        private async void AssociatedObjectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid)
            {
                DataGrid grid = sender as DataGrid;
                object item = grid?.SelectedItem;

                if (item != null)
                {
                    Action action = () =>
                    {
                        if (grid != null && item != null)
                        {
                            grid.UpdateLayout();
                            grid.ScrollIntoView(item, null);
                            DataGridRow row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(grid.SelectedIndex);
                            row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        }
                    };

                    if (grid.Dispatcher.CheckAccess())
                    {
                        action();
                    }
                    else
                    {
                        await grid.Dispatcher.BeginInvoke(action);
                    }
                }
            }
        }
    }
}