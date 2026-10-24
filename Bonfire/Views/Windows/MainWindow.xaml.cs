using System.Windows;

namespace Bonfire;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, e) =>
        {
            if (DataContext is Bonfire.ViewModels.MainWindowViewModel
                { CurrentViewModel: Bonfire.ViewModels.SeedsViewModel seeds }
                && !seeds.TryDiscardSeedChanges()) e.Cancel = true;
        };
    }
}
