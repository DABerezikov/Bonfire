using Bonfire.Domain.Garden;

namespace Models.Tests;

public class GardenGeometryEditTests
{
    private static CanvasRectangle Rect(double x) => CanvasRectangle.Create(x, 0, 90, 90).ValueOrThrow();
    private static GardenGeometryEdit Edit(double x = 120) => GardenGeometryEdit.Create(1, new GeometryTarget.Element(150), x, 0, 90, 90).ValueOrThrow();

    [Fact]
    public void ValidateAgainst_Locked_RefusesWithoutChangingOriginal()
    {
        var original = Rect(0);
        Assert.True(Edit().ValidateAgainst(original, true, new PlotLayout([])).IsFailure);
        Assert.Equal(0, original.X);
    }

    [Fact]
    public void ValidateAgainst_NewCollision_Refuses()
        => Assert.True(Edit().ValidateAgainst(Rect(0), false, new PlotLayout([Rect(120)])).IsFailure);

    [Fact]
    public void ValidateAgainst_ExistingCollision_AllowsMove()
        => Assert.True(Edit(30).ValidateAgainst(Rect(0), false, new PlotLayout([Rect(45)])).IsSuccess);

    [Fact]
    public void Create_Element_ComputesAreaWithoutRoundingGeometry()
    {
        var edit = Assert.IsType<GardenGeometryEdit.Element>(GardenGeometryEdit.Create(1, new GeometryTarget.Element(150), 120, 0, 180, 90).ValueOrThrow());
        Assert.Equal(0.7, edit.AreaSquareMeters);
        Assert.Equal(180, edit.Rectangle.Width);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.Epsilon)]
    [InlineData(double.MaxValue)]
    public void Create_InvalidScale_Refuses(double scale)
        => Assert.True(GardenGeometryEdit.Create(1, new GeometryTarget.Element(scale), 0, 0, 90, 90).IsFailure);

    [Theory]
    [InlineData(0, 90)]
    [InlineData(-1, 90)]
    [InlineData(1, -90)]
    [InlineData(1, double.PositiveInfinity)]
    public void Create_InvalidIdOrGeometry_Refuses(int id, double width)
        => Assert.True(GardenGeometryEdit.Create(id, new GeometryTarget.Greenhouse(), 0, 0, width, 90).IsFailure);
}
