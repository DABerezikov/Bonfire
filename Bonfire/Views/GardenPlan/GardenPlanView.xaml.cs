using System.Windows.Controls;
using System.Windows.Input;
using Bonfire.ViewModels;

namespace Bonfire.Views.GardenPlan;

public partial class GardenPlanView : UserControl
{
    public GardenPlanView()
    {
        InitializeComponent();
        // PreviewKeyDown — туннелирующее событие: срабатывает при фокусе в любом дочернем элементе.
        // Фокус сам View получает в двух случаях:
        //   1. Клик по пустому холсту → GardenCanvas_MouseLeftButtonDown
        //   2. Начало drag на элементе → GardenElementControl.OnDragStarted вызывает Focus()
        PreviewKeyDown += OnPreviewKeyDown;
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
