using System.Windows;
using System.Windows.Controls;
using Bonfire.ViewModels;

namespace Bonfire.Views.Planning;

public partial class PlanningLibrariesView : UserControl
{
    public PlanningLibrariesView() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PlanningLibrariesViewModel vm && vm.Regions.Count == 0) await vm.LoadAsync();
    }
}