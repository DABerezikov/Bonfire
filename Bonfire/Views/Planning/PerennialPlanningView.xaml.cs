using System.Windows;
using System.Windows.Controls;
using Bonfire.ViewModels;

namespace Bonfire.Views.Planning;

public partial class PerennialPlanningView : UserControl
{
    public PerennialPlanningView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PerennialPlanningViewModel vm && vm.EnsureLoadedCommand.CanExecute(null))
            vm.EnsureLoadedCommand.Execute(null);
    }
}