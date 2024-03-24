using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace Bonfire.Templates;

public class SingleRowDetailsBehavior : Behavior<DataGrid>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.LoadingRowDetails += OnLoadingRowDetails;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        this.AssociatedObject.LoadingRowDetails -= OnLoadingRowDetails;
    }

    private void OnLoadingRowDetails(object sender, DataGridRowDetailsEventArgs e)
    {
        var dataGrid = this.AssociatedObject;
        foreach (var item in dataGrid.Items)
        {
            var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(item);
            if (row != null && row != e.Row)
            {
                row.DetailsVisibility = Visibility.Collapsed;
            }
        }
    }
}