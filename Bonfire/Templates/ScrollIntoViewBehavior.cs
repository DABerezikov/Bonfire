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
            if (sender is not DataGrid grid) return;
            var item = grid?.SelectedItem;

            if (item == null) return;

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
                if (grid == null || item == null) return;
                grid.UpdateLayout();
                grid.ScrollIntoView(item, null);
                var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(grid.SelectedIndex);
                row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
    }
}