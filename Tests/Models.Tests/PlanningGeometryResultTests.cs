using Bonfire.Models.Mappers;

namespace Models.Tests;

public class PlanningGeometryResultTests
{
    [Theory]
    [InlineData(double.MaxValue, 1)]
    [InlineData(double.PositiveInfinity, 1)]
    [InlineData(1, double.Epsilon)]
    [InlineData(double.Epsilon, 1)]
    public void InvalidOrUnrepresentableDimensions_ReturnFailure(double width, double scale)
        => Assert.True(CanvasGeometryMapper.PlanningSize(width, 1, scale).IsFailure);

    [Fact]
    public void ValidDimensions_KeepOriginalMeterConversion()
    {
        var dimensions = CanvasGeometryMapper.PlanningSize(300, 135, 150).ValueOrThrow();
        Assert.Equal(2m, dimensions.Length); Assert.Equal(.9m, dimensions.Width);
    }
}
