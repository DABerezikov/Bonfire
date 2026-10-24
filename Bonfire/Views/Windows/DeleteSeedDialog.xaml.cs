using System.Windows;

namespace Bonfire.Views.Windows;

public partial class DeleteSeedDialog : Window
{
    public DeleteSeedDialog() => InitializeComponent();

    private void ConfirmDelete_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
