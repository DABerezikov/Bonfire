using System.Windows;
using Bonfire.ViewModels;

namespace Bonfire.Views.Windows;

public partial class DeleteSeedlingDialog : Window
{
    public DeleteSeedlingDialog() => InitializeComponent();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is DeleteSeedlingDialogViewModel previous) previous.Confirmed -= Confirm;
        if (e.NewValue is DeleteSeedlingDialogViewModel current) current.Confirmed += Confirm;
    }

    private void Confirm() => DialogResult = true;
}
