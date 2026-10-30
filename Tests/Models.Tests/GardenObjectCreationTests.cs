using Bonfire.Domain.Garden;

namespace Models.Tests;

public class GardenObjectCreationTests
{
    [Fact]
    public void Create_ValidElement_RequiresFreePositionWithinContainer()
    {
        var creation = GardenObjectCreation.CreateElement(1, "Имя", new GardenElementKind.Bed(), 0, 0, 300, 135).ValueOrThrow();
        Assert.True(creation.RequirePlacement(CanvasRectangle.Create(0, 0, 600, 300).ValueOrThrow(), new PlotLayout([])).IsSuccess);
        Assert.True(creation.RequirePlacement(CanvasRectangle.Create(0, 0, 100, 300).ValueOrThrow(), new PlotLayout([])).IsFailure);
        Assert.True(creation.RequirePlacement(CanvasRectangle.Create(0, 0, 600, 300).ValueOrThrow(), new PlotLayout([creation.Rectangle])).IsFailure);
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, double.NaN, 1)]
    [InlineData(1, 0, -1)]
    public void Create_InvalidElement_ReturnsFailure(int id, double x, double width)
        => Assert.True(GardenObjectCreation.CreateElement(id, "", new GardenElementKind.Bed(), x, 0, width, 1).IsFailure);

    [Fact]
    public void Create_Greenhouse_PreservesZeroSizeAndText()
    {
        var creation = GardenObjectCreation.CreateGreenhouse(1, "  ", 0, 0, 150, 0, 0).ValueOrThrow();
        Assert.Equal("  ", creation.Name); Assert.Equal(0, creation.Rectangle.Width); Assert.Equal(0, creation.Dimensions.WidthMeters);
    }
}
