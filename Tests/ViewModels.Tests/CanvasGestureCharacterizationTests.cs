using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using Bonfire.Views.GardenPlan;

namespace ViewModels.Tests;

public partial class CanvasInteractionTests
{
    public sealed class GestureCommands
    {
        public ICommand SelectElementCommand { get; init; } = new LambdaCommand(() => { });
        public ICommand SaveElementPositionCommand { get; init; } = new LambdaCommand(() => { });
        public ICommand SaveGreenhousePositionCommand { get; init; } = new LambdaCommand(() => { });
    }

    private static (FrameworkElement View, Thumb Move, Thumb Resize) Controls(object model)
    {
        FrameworkElement view = model is GreenhouseFromViewModel
            ? new GreenhouseControl { DataContext = model }
            : new GardenElementControl { DataContext = model };
        WpfRuntime.Layout(view, 200, 200);
        var thumbs = WpfRuntime.Descendants<Thumb>(view).ToArray();
        var move = thumbs.Single(thumb => model is GreenhouseFromViewModel ? thumb.Width != 10 : thumb.Name == "DragThumb");
        return (view, move, thumbs.Single(thumb => thumb != move));
    }

    private static void Drag(Thumb thumb, double x, double y)
    {
        thumb.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        thumb.RaiseEvent(new DragDeltaEventArgs(x, y) { RoutedEvent = Thumb.DragDeltaEvent });
        thumb.RaiseEvent(new DragCompletedEventArgs(x, y, false) { RoutedEvent = Thumb.DragCompletedEvent });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Drop_WithoutCollision_DoesNotRepeatPropertyChanges(bool greenhouse, bool resize) => runtime.Invoke(() =>
    {
        var element = new GardenElementFromViewModel { Width = 90, Height = 90 };
        var house = new GreenhouseFromViewModel { DisplayWidth = 150, DisplayHeight = 150 };
        var controls = Controls(greenhouse ? house : element);
        var thumb = resize ? controls.Resize : controls.Move;
        thumb.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        thumb.RaiseEvent(new DragDeltaEventArgs(30, 30) { RoutedEvent = Thumb.DragDeltaEvent });
        var notifications = 0;
        if (greenhouse) house.PropertyChanged += (_, _) => notifications++;
        else element.PropertyChanged += (_, _) => notifications++;
        thumb.RaiseEvent(new DragCompletedEventArgs(30, 30, false) { RoutedEvent = Thumb.DragCompletedEvent });
        Assert.Equal(0, notifications);
    });

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Drop_Collision_IsCorrectedBeforeXamlSaveCommand(bool greenhouse, bool resize) => runtime.Invoke(() =>
    {
        var element = new GardenElementFromViewModel { Width = 90, Height = 90 };
        var house = new GreenhouseFromViewModel { DisplayWidth = 150, DisplayHeight = 150 };
        var other = new GardenElementFromViewModel { X = 180, Width = 90, Height = 300 };
        element.ContainerElements = [element, other];
        house.ContainerElements = [other];
        var controls = Controls(greenhouse ? house : element);
        var saved = 0;
        var command = new LambdaCommand(parameter =>
        {
            Assert.Same(greenhouse ? (object)house : element, parameter);
            Assert.Equal(0, greenhouse ? house.X : element.X);
            Assert.Equal(greenhouse ? 150 : 90, greenhouse ? house.DisplayWidth : element.Width);
            saved++;
        });
        controls.View.Tag = new GestureCommands { SaveElementPositionCommand = command, SaveGreenhousePositionCommand = command };
        var window = new Window { Content = controls.View, Width = 400, Height = 400, Opacity = 0, ShowInTaskbar = false,
            WindowStyle = WindowStyle.None, Left = -32000, Top = -32000 };
        try
        {
            window.Show();
            WpfRuntime.Layout(controls.View, 200, 200);
            Drag(resize ? controls.Resize : controls.Move, resize ? 100 : 180, 0);
            Assert.Equal(1, saved);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Drag_SnapsAfterClampAtNonIntegralEdge(bool greenhouse) => runtime.Invoke(() =>
    {
        var element = new GardenElementFromViewModel { Width = 90, Height = 90, ContainerCanvasWidth = 208 };
        var house = new GreenhouseFromViewModel { DisplayWidth = 90, DisplayHeight = 90, ContainerCanvasWidth = 208 };
        var controls = Controls(greenhouse ? house : element);
        Drag(controls.Move, 1000, 0);
        Assert.Equal(120, greenhouse ? house.X : element.X);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Drag_PreexistingCollision_DoesNotPreventMovement(bool greenhouse) => runtime.Invoke(() =>
    {
        var element = new GardenElementFromViewModel { X = 150, Width = 90, Height = 90 };
        var house = new GreenhouseFromViewModel { X = 150, DisplayWidth = 90, DisplayHeight = 90 };
        var other = new GardenElementFromViewModel { X = 150, Width = 90, Height = 90 };
        element.ContainerElements = [element, other];
        house.ContainerElements = [other];
        house.ContainerGreenhouses = [house];
        Drag(Controls(greenhouse ? house : element).Move, 15, 0);
        Assert.Equal(165, greenhouse ? house.X : element.X);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resize_NewCollision_RestoresExactOriginalDimensions(bool greenhouse) => runtime.Invoke(() =>
    {
        var element = new GardenElementFromViewModel { Width = 90, Height = 90 };
        var house = new GreenhouseFromViewModel { DisplayWidth = 150, DisplayHeight = 90 };
        var other = new GardenElementFromViewModel { X = 180, Width = 90, Height = 300 };
        element.ContainerElements = [element, other];
        house.ContainerElements = [other];
        Drag(Controls(greenhouse ? house : element).Resize, 100, 100);
        Assert.Equal(greenhouse ? 150 : 90, greenhouse ? house.DisplayWidth : element.Width);
        Assert.Equal(90, greenhouse ? house.DisplayHeight : element.Height);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resize_MinimumHasPriorityOverSmallContainer(bool greenhouse) => runtime.Invoke(() =>
    {
        var element = new GardenElementFromViewModel { Width = 90, Height = 90, ContainerCanvasWidth = 10, ContainerCanvasHeight = 10 };
        var house = new GreenhouseFromViewModel { DisplayWidth = 90, DisplayHeight = 90, ContainerCanvasWidth = 100, ContainerCanvasHeight = 100 };
        Drag(Controls(greenhouse ? house : element).Resize, -1000, -1000);
        Assert.Equal(greenhouse ? 150 : 15, greenhouse ? house.DisplayWidth : element.Width);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resize_LockedObject_PreservesSize(bool greenhouse) => runtime.Invoke(() =>
    {
        var element = new GardenElementFromViewModel { Width = 90, Height = 90, IsLocked = true };
        var house = new GreenhouseFromViewModel { DisplayWidth = 90, DisplayHeight = 90, IsLocked = true };
        Drag(Controls(greenhouse ? house : element).Resize, 100, 100);
        Assert.Equal(90, greenhouse ? house.DisplayWidth : element.Width);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Drag_ActualCanvasScale_DividesWindowDelta(bool greenhouse) => runtime.Invoke(() =>
    {
        var element = new GardenElementFromViewModel { Width = 90, Height = 90 };
        var house = new GreenhouseFromViewModel { DisplayWidth = 90, DisplayHeight = 90 };
        var controls = Controls(greenhouse ? house : element);
        var canvas = new Canvas { Width = 300, Height = 300, RenderTransform = new ScaleTransform(2, 2) };
        canvas.Children.Add(controls.View);
        var window = new Window { Content = canvas, Width = 640, Height = 640, Opacity = 0, ShowInTaskbar = false,
            WindowStyle = WindowStyle.None, Left = -32000, Top = -32000 };
        try
        {
            window.Show();
            WpfRuntime.Layout(canvas, 300, 300);
            Drag(controls.Move, 60, 90);
            Assert.Equal(30, greenhouse ? house.X : element.X);
            Assert.Equal(45, greenhouse ? house.Y : element.Y);
        }
        finally { window.Close(); }
    });
}
