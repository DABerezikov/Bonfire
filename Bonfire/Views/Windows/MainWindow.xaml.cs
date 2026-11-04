using System.Windows;
using Bonfire.ViewModels;

namespace Bonfire;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _shutdownCommitted;

    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, e) =>
        {
            if (_shutdownCommitted) return;
            if (DataContext is MainWindowViewModel
                {
                    CurrentViewModel: SeedsViewModel seeds
                }
                && !seeds.TryDiscardSeedChanges()) e.Cancel = true;
        };
    }

    internal void CompleteShutdown()
    {
        _shutdownCommitted = true;
        Close();
    }
}