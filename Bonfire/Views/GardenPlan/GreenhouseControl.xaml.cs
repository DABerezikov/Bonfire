using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Bonfire.Models;

namespace Bonfire.Views.GardenPlan;

public partial class GreenhouseControl : UserControl
{
    public GreenhouseControl()
    {
        InitializeComponent();
    }

    private void DragThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GreenhouseFromViewModel vm) return;

        double maxX = vm.ContainerCanvasWidth  > 0 ? vm.ContainerCanvasWidth  - vm.DisplayWidth  : double.MaxValue;
        double maxY = vm.ContainerCanvasHeight > 0 ? vm.ContainerCanvasHeight - vm.DisplayHeight : double.MaxValue;

        vm.X = Math.Max(0, Math.Min(vm.X + e.HorizontalChange, maxX));
        vm.Y = Math.Max(0, Math.Min(vm.Y + e.VerticalChange,   maxY));
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GreenhouseFromViewModel vm) return;

        double maxW = vm.ContainerCanvasWidth  > 0 ? vm.ContainerCanvasWidth  - vm.X : double.MaxValue;
        double maxH = vm.ContainerCanvasHeight > 0 ? vm.ContainerCanvasHeight - vm.Y : double.MaxValue;

        vm.DisplayWidth  = Math.Max(80, Math.Min(vm.DisplayWidth  + e.HorizontalChange, maxW));
        vm.DisplayHeight = Math.Max(60, Math.Min(vm.DisplayHeight + e.VerticalChange,   maxH));
    }
}
