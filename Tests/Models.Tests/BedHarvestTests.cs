using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;

namespace Models.Tests;

public class BedHarvestTests
{
    [Theory]
    [InlineData(false, 5)]
    [InlineData(true, 6)]
    public void Capacity_CountsRowsRatherThanArea(bool across, int expected)
    {
        var value = BedCapacity.Calculate(Layout(across)).ValueOrThrow();
        Assert.Equal(expected, value.Plants);
        Assert.Equal(1.8m, value.AreaM2);
    }

    [Fact]
    public void Capacity_RejectsZeroAndOverflowAndAllowsNoSpace()
    {
        Assert.True(BedCapacity.Calculate(Layout() with { PlantStep = 0 }).IsFailure);
        Assert.True(BedCapacity.Calculate(Layout() with { Length = decimal.MaxValue }).IsFailure);
        Assert.Equal(0, BedCapacity.Calculate(Layout() with { StartMargin = 3 }).ValueOrThrow().Plants);
        Assert.Equal(6, BedCapacity.Calculate(Layout() with { StartMargin = 0, EndMargin = 0 }).ValueOrThrow().Plants);
    }

    [Fact]
    public void Projection_IndependentNormsAndTargetDoNotClampToBed()
    {
        var input = new HarvestPlanningInput { Layout = Option.Some(Layout()), PerPlantKg = Option.Some(2m),
            PerM2Kg = Option.Some(4m), YieldAreaM2 = Option.Some(1.8m), TargetKg = Option.Some(15m), Mode = HarvestPlanningMode.TargetPerPlant };
        var result = HarvestProjection.Calculate(input, 20).ValueOrThrow();
        Assert.Equal(8, result.TargetPlants);
        Assert.Equal(Option.Some(3), result.PlantDeficit);
        Assert.Equal(Option.Some(16m), result.PerPlantKg);
        Assert.Equal(Option.Some(7.2m), result.PerM2Kg);
    }

    [Fact]
    public void Projection_M2UsesExplicitAreaAndMarksDensityEstimate()
    {
        var input = new HarvestPlanningInput { Layout = Option.Some(Layout()), PerM2Kg = Option.Some(4m),
            YieldAreaM2 = Option.Some(1.8m), TargetKg = Option.Some(12m), Mode = HarvestPlanningMode.TargetPerM2 };
        var result = HarvestProjection.Calculate(input, 20).ValueOrThrow();
        Assert.Equal(13, result.TargetPlants);
        Assert.Equal(Option.Some(3m), result.RequiredAreaM2);
        Assert.Equal(Option.Some(1.2m), result.AreaDeficitM2);
        Assert.True(result.UsesDensityEstimate);
        Assert.True(HarvestProjection.Calculate(input with { PerM2Kg = Option.Some(0m) }, 20).IsFailure);
        Assert.True(HarvestProjection.Calculate(input with { PerM2Kg = Option.Some(-1m) }, 20).IsFailure);
    }

    internal static BedLayout Layout(bool across = false) => new(2m, .9m, .4m, .6m, .2m, .2m, .3m, .3m, across);
}
