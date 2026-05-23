using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Bonfire.Infrastructure;
using Bonfire.Models;

namespace Bonfire.Views.GardenPlan;

public partial class GreenhouseControl : UserControl
{
    private double _preDragX, _preDragY;
    private double _preDragW, _preDragH;

    public GreenhouseControl()
    {
        InitializeComponent();
    }

    private void DragThumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        if (DataContext is GreenhouseFromViewModel vm)
        {
            _preDragX = vm.X;           _preDragY = vm.Y;
            _preDragW = vm.DisplayWidth; _preDragH = vm.DisplayHeight;
        }
    }

    private void DragThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not GreenhouseFromViewModel vm) return;
        bool wasColliding = CollisionHelper.CollidesWithSiblings(_preDragX, _preDragY, vm.DisplayWidth, vm.DisplayHeight, vm);
        bool isColliding  = CollisionHelper.CollidesWithSiblings(vm.X, vm.Y, vm.DisplayWidth, vm.DisplayHeight, vm);
        if (isColliding && !wasColliding) { vm.X = _preDragX; vm.Y = _preDragY; }
    }

    private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not GreenhouseFromViewModel vm) return;
        bool wasColliding = CollisionHelper.CollidesWithSiblings(vm.X, vm.Y, _preDragW, _preDragH, vm);
        bool isColliding  = CollisionHelper.CollidesWithSiblings(vm.X, vm.Y, vm.DisplayWidth, vm.DisplayHeight, vm);
        if (isColliding && !wasColliding) { vm.DisplayWidth = _preDragW; vm.DisplayHeight = _preDragH; }
    }

    private void DragThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GreenhouseFromViewModel vm || vm.IsLocked) return;

        const double snap = 15; // 0.1 м = 15 пкс
        double scale = GetCanvasScale(this);
        double maxX = vm.ContainerCanvasWidth  > 0 ? vm.ContainerCanvasWidth  - vm.DisplayWidth  : double.MaxValue;
        double maxY = vm.ContainerCanvasHeight > 0 ? vm.ContainerCanvasHeight - vm.DisplayHeight : double.MaxValue;

        vm.X = Math.Round(Math.Max(0, Math.Min(vm.X + e.HorizontalChange / scale, maxX)) / snap) * snap;
        vm.Y = Math.Round(Math.Max(0, Math.Min(vm.Y + e.VerticalChange   / scale, maxY)) / snap) * snap;
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GreenhouseFromViewModel vm || vm.IsLocked) return;

        double scale = GetCanvasScale(this);
        double maxW = vm.ContainerCanvasWidth  > 0 ? vm.ContainerCanvasWidth  - vm.X : double.MaxValue;
        double maxH = vm.ContainerCanvasHeight > 0 ? vm.ContainerCanvasHeight - vm.Y : double.MaxValue;

        vm.DisplayWidth  = Math.Round(Math.Max(150, Math.Min(vm.DisplayWidth  + e.HorizontalChange / scale, maxW)));
        vm.DisplayHeight = Math.Round(Math.Max(150, Math.Min(vm.DisplayHeight + e.VerticalChange   / scale, maxH)));
    }

    private static double GetCanvasScale(UIElement element)
    {
        try
        {
            DependencyObject current = element;
            while (true)
            {
                var parent = VisualTreeHelper.GetParent(current);
                if (parent is null) return 1.0;
                if (parent is Canvas canvas)
                {
                    var window = Window.GetWindow(canvas);
                    if (window is null) return 1.0;
                    var t      = canvas.TransformToAncestor(window);
                    var origin = t.Transform(new Point(0, 0));
                    var unitX  = t.Transform(new Point(1, 0));
                    var scale  = unitX.X - origin.X;
                    return scale > 0 ? scale : 1.0;
                }
                current = parent;
            }
        }
        catch { return 1.0; }
    }
}
