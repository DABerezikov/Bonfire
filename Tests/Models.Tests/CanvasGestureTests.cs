using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Models.Tests;

public class CanvasGestureTests
{
    private static CanvasRectangle Rect(double x = 0, double width = 90)
        => CanvasRectangle.Create(x, 0, width, 90).ValueOrThrow();
    private static CanvasBounds Bounds(double width = 600)
        => CanvasBounds.Create(Option.Some(width), Option.Some(600.0)).ValueOrThrow();

    [Theory]
    [InlineData(31, 46, 1, 30, 45)]
    [InlineData(60, 90, 2, 30, 45)]
    [InlineData(7.5, 22.5, 1, 0, 30)]
    [InlineData(1000, -1000, 1, 510, 0)]
    public void WithMoved_ScalesClampsThenSnapsToEvenAndPreservesOriginal(double dx, double dy, double scale, double x, double y)
    {
        var original = Rect();
        var moved = original.WithMoved(dx, dy, scale, Bounds()).ValueOrThrow();
        Assert.Equal((x, y), (moved.X, moved.Y));
        Assert.Equal((90.0, 90.0), (moved.Width, moved.Height));
        Assert.Equal(0, original.X);
    }

    [Fact]
    public void WithMoved_SnapAfterClamp_PreservesSmallOverflowAtContainerEdge()
        => Assert.Equal(120, Rect().WithMoved(1000, 0, 1, Bounds(208)).ValueOrThrow().X);

    [Fact]
    public void WithMoved_UnknownBounds_DoesNotInventZeroLimit()
        => Assert.Equal(1005, Rect().WithMoved(1000, 0, 1,
            CanvasBounds.Create(Option.None<double>(), Option.None<double>()).ValueOrThrow()).ValueOrThrow().X);

    [Theory]
    [InlineData(-1000, 1, 15, 300, 15)]
    [InlineData(1000, 1, 15, 300, 300)]
    [InlineData(1, 2, 15, 300, 90)]
    [InlineData(3, 2, 15, 300, 92)]
    [InlineData(-1000, 1, 150, 100, 150)]
    public void WithResized_PreservesMinimumPriorityAndIntegerRounding(double dx, double scale, double minimum, double width, double expected)
    {
        var original = Rect();
        var resized = original.WithResized(dx, dx, scale, Bounds(width), minimum).ValueOrThrow();
        Assert.Equal(expected, resized.Width);
        Assert.Equal(90, original.Width);
        Assert.Equal(0, resized.X);
    }

    [Theory]
    [InlineData(0, 150, 0)]
    [InlineData(150, 165, 165)]
    [InlineData(0, 60, 60)]
    public void ResolveDropFrom_OnlyNewCollisionRestoresOriginal(double originalX, double targetX, double expected)
    {
        var layout = new PlotLayout([Rect(150)]);
        var original = Rect(originalX);
        var target = Rect(targetX);
        var resolved = target.ResolveDropFrom(original, layout.CollidesWith);
        Assert.Equal(expected, resolved.X);
        Assert.Equal(targetX, target.X);
    }

    [Fact]
    public void ResolveDropFrom_NewResizeCollision_RestoresOriginalSize()
    {
        var layout = new PlotLayout([Rect(150)]);
        Assert.Equal(90, Rect(width: 180).ResolveDropFrom(Rect(), layout.CollidesWith).Width);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void WithResized_InvalidMinimum_ReturnsFailure(double minimum)
        => Assert.True(Rect().WithResized(1, 1, 1, Bounds(), minimum).IsFailure);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.PositiveInfinity)]
    public void CreateBounds_InvalidKnownDimension_ReturnsFailure(double width)
        => Assert.True(CanvasBounds.Create(Option.Some(width), Option.None<double>()).IsFailure);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(double.NaN, 1)]
    [InlineData(double.PositiveInfinity, 1)]
    [InlineData(1, double.NaN)]
    public void WithMoved_InvalidScaleOrDelta_ReturnsFailure(double scale, double delta)
        => Assert.True(Rect().WithMoved(delta, 0, scale, Bounds()).IsFailure);
}
