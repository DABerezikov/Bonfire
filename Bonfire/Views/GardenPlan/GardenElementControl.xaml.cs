using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Infrastructure;
using Bonfire.Models;

namespace Bonfire.Views.GardenPlan;

public partial class GardenElementControl : UserControl
{
    private Option<CanvasRectangle> _beforeDrag = Option.None<CanvasRectangle>();

    public GardenElementControl()
    {
        InitializeComponent();
    }

    public void OnDragStarted(object sender, DragStartedEventArgs e)
    {
        Focus();
        _beforeDrag = DataContext is GardenElementFromViewModel model
            ? CanvasGestureAdapter.Capture(model) : Option.None<CanvasRectangle>();
    }

    public void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel model || model.IsLocked) return;
        CanvasGestureAdapter.Apply(CanvasGestureAdapter.Move(model, e.HorizontalChange, e.VerticalChange, CanvasScale.Get(this)),
            rectangle => { model.X = rectangle.X; model.Y = rectangle.Y; });
    }

    // XAML вызывает обработчик до команды сохранения — сохраняется скорректированная позиция.
    private void OnMoveDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel model) return;
        CanvasGestureAdapter.Apply(CanvasGestureAdapter.CompleteMove(model, _beforeDrag),
            rectangle => { model.X = rectangle.X; model.Y = rectangle.Y; });
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel model || model.IsLocked) return;
        CanvasGestureAdapter.Apply(CanvasGestureAdapter.Resize(model, e.HorizontalChange, e.VerticalChange, CanvasScale.Get(this)),
            rectangle => { model.Width = rectangle.Width; model.Height = rectangle.Height; });
    }

    private void OnResizeDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not GardenElementFromViewModel model) return;
        CanvasGestureAdapter.Apply(CanvasGestureAdapter.CompleteResize(model, _beforeDrag),
            rectangle => { model.Width = rectangle.Width; model.Height = rectangle.Height; });
    }
}
