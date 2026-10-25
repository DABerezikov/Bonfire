using System.Windows.Controls.Primitives;
using Bonfire.Views.GardenPlan;

namespace ViewModels.Tests;

[Collection("WPF runtime")]
public partial class CanvasInteractionTests(WpfRuntime runtime)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ElementDrag_SnapsToGridAndHonorsLock(bool locked) => runtime.Invoke(() =>
    {
        var model = new GardenElementFromViewModel { Width = 90, Height = 90, ContainerCanvasWidth = 600, ContainerCanvasHeight = 600, IsLocked = locked };
        var view = new GardenElementControl { DataContext = model };
        WpfRuntime.Layout(view, 100, 100);
        var thumb = WpfRuntime.Descendants<Thumb>(view).Single(t => t.Name == "DragThumb");
        thumb.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        thumb.RaiseEvent(new DragDeltaEventArgs(31, 46) { RoutedEvent = Thumb.DragDeltaEvent });
        Assert.Equal(locked ? 0 : 30, model.X);
        Assert.Equal(locked ? 0 : 45, model.Y);
    });

    [Fact]
    public void ElementDrag_CollisionAtDropRestoresOriginalPosition() => runtime.Invoke(() =>
    {
        var model = new GardenElementFromViewModel { Width = 90, Height = 90, ContainerCanvasWidth = 600, ContainerCanvasHeight = 600 };
        var other = new GardenElementFromViewModel { X = 150, Width = 90, Height = 90 };
        model.ContainerElements = [model, other];
        var view = new GardenElementControl { DataContext = model };
        WpfRuntime.Layout(view, 100, 100);
        var thumb = WpfRuntime.Descendants<Thumb>(view).Single(t => t.Name == "DragThumb");
        thumb.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        thumb.RaiseEvent(new DragDeltaEventArgs(150, 0) { RoutedEvent = Thumb.DragDeltaEvent });
        Assert.Equal(150, model.X);
        thumb.RaiseEvent(new DragCompletedEventArgs(150, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
        Assert.Equal(0, model.X);
    });

    [Theory]
    [InlineData(-1000, 15)]
    [InlineData(1000, 300)]
    public void ElementResize_StaysBetweenMinimumAndContainerBounds(double delta, double expected) => runtime.Invoke(() =>
    {
        var model = new GardenElementFromViewModel { Width = 90, Height = 90, ContainerCanvasWidth = 300, ContainerCanvasHeight = 300 };
        var view = new GardenElementControl { DataContext = model };
        WpfRuntime.Layout(view, 100, 100);
        var thumb = WpfRuntime.Descendants<Thumb>(view).Single(t => t.Name != "DragThumb");
        thumb.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        thumb.RaiseEvent(new DragDeltaEventArgs(delta, delta) { RoutedEvent = Thumb.DragDeltaEvent });
        Assert.Equal(expected, model.Width); Assert.Equal(expected, model.Height);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GreenhouseDrag_HonorsLockAndContainerBounds(bool locked) => runtime.Invoke(() =>
    {
        var model = new GreenhouseFromViewModel { DisplayWidth = 150, DisplayHeight = 90, ContainerCanvasWidth = 300, ContainerCanvasHeight = 300, IsLocked = locked };
        var view = new GreenhouseControl { DataContext = model };
        WpfRuntime.Layout(view, 160, 100);
        var thumb = WpfRuntime.Descendants<Thumb>(view).First(t => t.Width != 10);
        thumb.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
        thumb.RaiseEvent(new DragDeltaEventArgs(900, -100) { RoutedEvent = Thumb.DragDeltaEvent });
        Assert.Equal(locked ? 0 : 150, model.X); Assert.Equal(0, model.Y);
    });
}
