using System.Windows.Controls;
using System.Windows.Input;
using Bonfire.ViewModels;

namespace Bonfire.Views.GardenPlan;

public partial class GardenPlanView : UserControl
{
    public GardenPlanView()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void GardenScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            if (DataContext is GardenPlanViewModel vm)
                vm.GardenZoom += e.Delta > 0 ? 0.1 : -0.1;
            e.Handled = true;
        }
    }

    /// <summary>Клик по пустому месту холста участка — взять фокус для Escape.</summary>
    private void GardenCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        GardenCanvas.Focus();
    }

    /// <summary>Клик по пустому месту внутреннего холста теплицы — взять фокус для Escape.</summary>
    private void GreenhouseCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        GreenhouseCanvas.Focus();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is GardenPlanViewModel vm)
        {
            vm.DeselectElementCommand.Execute(null);
            e.Handled = true;
        }
    }
}
