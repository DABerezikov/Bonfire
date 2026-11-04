using System.Windows;
using System.Windows.Controls;
using Bonfire.ViewModels;

namespace Bonfire.Views.Planning;

public partial class PlanningLibrariesView : UserControl
{
    public PlanningLibrariesView() => InitializeComponent();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PlanningLibrariesViewModel vm && vm.EnsureLoadedCommand.CanExecute(null))
            vm.EnsureLoadedCommand.Execute(null);
    }
}