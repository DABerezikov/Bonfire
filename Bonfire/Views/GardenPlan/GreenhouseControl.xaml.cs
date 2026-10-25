using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Infrastructure;
using Bonfire.Models;

namespace Bonfire.Views.GardenPlan;

public partial class GreenhouseControl : UserControl
{
    private Option<CanvasRectangle> _beforeDrag = Option.None<CanvasRectangle>();

    public GreenhouseControl()
    {
        InitializeComponent();
    }

    private void DragThumb_DragStarted(object sender, DragStartedEventArgs e)
        => _beforeDrag = DataContext is GreenhouseFromViewModel model
            ? CanvasGestureAdapter.Capture(model) : Option.None<CanvasRectangle>();

    private void DragThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GreenhouseFromViewModel model || model.IsLocked) return;
        CanvasGestureAdapter.Apply(CanvasGestureAdapter.Move(model, e.HorizontalChange, e.VerticalChange, CanvasScale.Get(this)),
            rectangle => { model.X = rectangle.X; model.Y = rectangle.Y; });
    }

    private void DragThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not GreenhouseFromViewModel model) return;
        CanvasGestureAdapter.Apply(CanvasGestureAdapter.CompleteMove(model, _beforeDrag),
            rectangle => { model.X = rectangle.X; model.Y = rectangle.Y; });
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GreenhouseFromViewModel model || model.IsLocked) return;
        CanvasGestureAdapter.Apply(CanvasGestureAdapter.Resize(model, e.HorizontalChange, e.VerticalChange, CanvasScale.Get(this)),
            rectangle => { model.DisplayWidth = rectangle.Width; model.DisplayHeight = rectangle.Height; });
    }

    private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not GreenhouseFromViewModel model) return;
        CanvasGestureAdapter.Apply(CanvasGestureAdapter.CompleteResize(model, _beforeDrag),
            rectangle => { model.DisplayWidth = rectangle.Width; model.DisplayHeight = rectangle.Height; });
    }
}
