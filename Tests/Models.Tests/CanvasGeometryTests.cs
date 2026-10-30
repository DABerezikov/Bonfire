using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Models.Tests;

public class CanvasGeometryTests
{
    private static CanvasRectangle Rect(double x, double y, double width = 100, double height = 80)
        => CanvasRectangle.Create(x, y, width, height).ValueOrThrow();

    [Theory]
    [InlineData(100, 0, false)]
    [InlineData(0, 80, false)]
    [InlineData(-100, 0, false)]
    [InlineData(0, -80, false)]
    [InlineData(99, 0, true)]
    [InlineData(0, 79, true)]
    [InlineData(0, 0, true)]
    [InlineData(500, 500, false)]
    public void Overlaps_PreservesStrictEdgesAndSymmetry(double x, double y, bool overlaps)
    {
        var first = Rect(0, 0);
        var second = Rect(x, y);
        Assert.Equal(overlaps, first.Overlaps(second));
        Assert.Equal(overlaps, second.Overlaps(first));
    }

    [Fact]
    public void FindFreeSpot_PreservesTopToBottomLeftToRightOrderingAndSnapshot()
    {
        var obstacles = new List<CanvasRectangle> { Rect(0, 0) };
        var layout = new PlotLayout(obstacles);
        obstacles.Clear();
        var found = layout.FindFreeSpot(100, 80, 300, 160).ValueOrThrow().Match(rectangle => rectangle,
            () => throw new InvalidOperationException("Место должно быть найдено"));
        Assert.Equal((100.0, 0.0), (found.X, found.Y));
        Assert.Single(layout.Obstacles);
        Assert.False(layout.CollidesWith(found));
    }

    [Fact]
    public void FindFreeSpot_NoSpace_ReturnsNoneInsideSuccess()
        => Assert.IsType<Option<CanvasRectangle>.None>(new PlotLayout([Rect(0, 0)]).FindFreeSpot(100, 80, 100, 80).ValueOrThrow());

    [Fact]
    public void FindFreeSpot_ExistingNegativePosition_IsPreservedAndBlocksItsVisiblePart()
    {
        var found = Assert.IsType<Option<CanvasRectangle>.Some>(new PlotLayout([Rect(-50, 0)])
            .FindFreeSpot(50, 80, 100, 80).ValueOrThrow()).Value;
        Assert.Equal((50.0, 0.0), (found.X, found.Y));
    }

    [Fact]
    public void Overlaps_ZeroSizedRectangleInsideAnother_PreservesLegacyComparison()
        => Assert.True(Rect(50, 50, 0, 0).Overlaps(Rect(0, 0)));

    [Fact]
    public void FindFreeSpot_EmptyCanvasAndZeroSizedItem_PreservesOrigin()
    {
        var found = Assert.IsType<Option<CanvasRectangle>.Some>(new PlotLayout([]).FindFreeSpot(0, 0, 0, 0).ValueOrThrow()).Value;
        Assert.Equal((0.0, 0.0), (found.X, found.Y));
    }

    [Theory]
    [InlineData(double.NaN, 80, 300, 200)]
    [InlineData(100, 80, double.NaN, 200)]
    [InlineData(100, 80, 300, double.PositiveInfinity)]
    [InlineData(-1, 80, 300, 200)]
    public void FindFreeSpot_InvalidGeometry_ReturnsFailure(double width, double height, double canvasWidth, double canvasHeight)
        => Assert.True(new PlotLayout([]).FindFreeSpot(width, height, canvasWidth, canvasHeight).IsFailure);

    [Theory]
    [InlineData(double.NaN, 0, 1, 1)]
    [InlineData(0, double.PositiveInfinity, 1, 1)]
    [InlineData(0, 0, -1, 1)]
    [InlineData(0, 0, 1, -1)]
    [InlineData(double.MaxValue, 0, double.MaxValue, 1)]
    public void Create_InvalidRectangle_ReturnsFailure(double x, double y, double width, double height)
        => Assert.True(CanvasRectangle.Create(x, y, width, height).IsFailure);
}
