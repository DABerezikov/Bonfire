using System.Windows;
using System.Windows.Controls;
using Bonfire.ViewModels;

namespace Bonfire.Views.Planning;

public partial class HarvestJournalView : UserControl
{
    public HarvestJournalView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is HarvestJournalViewModel vm && vm.EnsureLoadedCommand.CanExecute(null))
            vm.EnsureLoadedCommand.Execute(null);
    }
}