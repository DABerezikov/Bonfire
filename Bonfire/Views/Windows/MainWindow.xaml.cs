using System.Windows;

namespace Bonfire;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _shutdownCommitted;

    internal void CompleteShutdown()
    {
        _shutdownCommitted = true;
        Close();
    }

    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, e) =>
        {
            if (_shutdownCommitted) return;
            if (DataContext is Bonfire.ViewModels.MainWindowViewModel
                { CurrentViewModel: Bonfire.ViewModels.SeedsViewModel seeds }
                && !seeds.TryDiscardSeedChanges()) e.Cancel = true;
        };
    }
}
